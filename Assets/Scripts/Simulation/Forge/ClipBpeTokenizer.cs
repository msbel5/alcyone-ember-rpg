#if USE_ONNX_RUNTIME
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace EmberCrpg.Simulation.Forge
{
    /// <summary>
    /// Pure-C# implementation of the CLIP BPE tokenizer used by Stable Diffusion
    /// (1.5 / SDXL). Reads vocab.json + merges.txt from a HuggingFace
    /// <c>tokenizer/</c> folder and produces fixed-length 77-token ID sequences
    /// with BOS / EOS padding.
    /// </summary>
    /// <remarks>
    /// Mirrors openai/CLIP <c>simple_tokenizer.py</c>:
    /// <list type="number">
    /// <item>Lowercase + whitespace normalize the prompt.</item>
    /// <item>Apply the CLIP pre-tokenizer regex.</item>
    /// <item>Convert each pre-token's UTF-8 bytes via bytes_to_unicode and
    /// append <c>&lt;/w&gt;</c> to the last symbol.</item>
    /// <item>Apply greedy BPE merges in rank order.</item>
    /// <item>Map sub-tokens through the vocab.</item>
    /// <item>Prepend BOS (49406), append EOS (49407), pad with EOS to maxLength.</item>
    /// </list>
    /// Kept inside the <c>noEngineReferences=true</c> simulation assembly so it
    /// has no UnityEngine dependency.
    /// </remarks>
    internal sealed class ClipBpeTokenizer
    {
        // CLIP pre-tokenizer pattern. Matches contractions, letter runs, digits,
        // and any non-whitespace symbol run. .NET regex Unicode classes are
        // active by default.
        private static readonly Regex Pattern = new Regex(
            @"<\|startoftext\|>|<\|endoftext\|>|'s|'t|'re|'ve|'m|'ll|'d|[\p{L}]+|[\p{N}]|[^\s\p{L}\p{N}]+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex WhitespaceCollapse = new Regex(@"\s+", RegexOptions.Compiled);

        private readonly Dictionary<string, int> _encoder;
        private readonly Dictionary<(string, string), int> _bpeRanks;
        private readonly Dictionary<byte, char> _byteEncoder;

        private ClipBpeTokenizer(
            Dictionary<string, int> encoder,
            Dictionary<(string, string), int> ranks,
            Dictionary<byte, char> byteEncoder)
        {
            _encoder = encoder;
            _bpeRanks = ranks;
            _byteEncoder = byteEncoder;
        }

        /// <summary>
        /// Loads a CLIP BPE tokenizer from a HuggingFace tokenizer folder. The
        /// <paramref name="vocabJsonPath"/> argument points at the vocab.json
        /// file; merges.txt is read from the same directory.
        /// </summary>
        public static ClipBpeTokenizer LoadFromVocab(string vocabJsonPath)
        {
            if (string.IsNullOrEmpty(vocabJsonPath))
                throw new ArgumentException("vocab.json path is required.", nameof(vocabJsonPath));
            if (!File.Exists(vocabJsonPath))
                throw new FileNotFoundException("vocab.json not found", vocabJsonPath);

            var tokenizerDir = Path.GetDirectoryName(vocabJsonPath);
            if (string.IsNullOrEmpty(tokenizerDir))
                throw new InvalidOperationException("Cannot resolve tokenizer directory from vocab path.");

            var mergesPath = Path.Combine(tokenizerDir, "merges.txt");
            if (!File.Exists(mergesPath))
                throw new FileNotFoundException("merges.txt not found alongside vocab.json", mergesPath);

            var encoder = ParseVocabJson(File.ReadAllText(vocabJsonPath));
            var ranks = ParseMergesTxt(File.ReadAllLines(mergesPath));
            var byteEncoder = BuildBytesToUnicode();
            return new ClipBpeTokenizer(encoder, ranks, byteEncoder);
        }

        /// <summary>
        /// Tokenize the prompt to a fixed-length sequence of long IDs ready to
        /// feed into an ONNX CLIP text encoder.
        /// </summary>
        public long[] Tokenize(string prompt, int maxLength, int bosId, int eosId)
        {
            if (maxLength < 2)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "Need room for at least BOS + EOS.");

            var ids = new List<long>(maxLength) { bosId };

            string normalized = WhitespaceCollapse
                .Replace((prompt ?? string.Empty).Trim(), " ")
                .ToLowerInvariant();

            foreach (Match m in Pattern.Matches(normalized))
            {
                if (ids.Count >= maxLength - 1) break;

                string token = m.Value;
                var sb = new StringBuilder(token.Length * 2);
                var bytes = Encoding.UTF8.GetBytes(token);
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (_byteEncoder.TryGetValue(bytes[i], out char c))
                        sb.Append(c);
                }
                sb.Append("</w>");

                var bpeTokens = BpeEncode(sb.ToString());
                foreach (var t in bpeTokens)
                {
                    if (ids.Count >= maxLength - 1) break;
                    if (_encoder.TryGetValue(t, out int id))
                        ids.Add(id);
                    // Out-of-vocab sub-tokens are silently skipped — the
                    // openai reference implementation likewise relies on the
                    // BPE merges covering every byte.
                }
            }

            ids.Add(eosId);
            while (ids.Count < maxLength) ids.Add(eosId);
            return ids.ToArray();
        }

        // --- BPE core ---

        private List<string> BpeEncode(string word)
        {
            // Split the byte-encoded word into a list of symbols. The trailing
            // "</w>" suffix is attached to the final character so the BPE
            // greedy merge sees a single terminal symbol.
            var symbols = new List<string>();
            const string EndOfWord = "</w>";
            if (word.EndsWith(EndOfWord))
            {
                string head = word.Substring(0, word.Length - EndOfWord.Length);
                if (head.Length == 0)
                {
                    symbols.Add(EndOfWord);
                }
                else
                {
                    for (int i = 0; i < head.Length - 1; i++) symbols.Add(head[i].ToString());
                    symbols.Add(head[head.Length - 1] + EndOfWord);
                }
            }
            else
            {
                foreach (var c in word) symbols.Add(c.ToString());
            }

            if (symbols.Count <= 1) return symbols;

            // Greedy BPE merge: repeatedly merge ALL occurrences of the lowest
            // ranked adjacent pair until no merge candidate remains.
            while (symbols.Count > 1)
            {
                (string a, string b) bestPair = (null, null);
                int bestRank = int.MaxValue;
                for (int i = 0; i < symbols.Count - 1; i++)
                {
                    var pair = (symbols[i], symbols[i + 1]);
                    if (_bpeRanks.TryGetValue(pair, out int r) && r < bestRank)
                    {
                        bestRank = r;
                        bestPair = pair;
                    }
                }
                if (bestRank == int.MaxValue) break;

                var next = new List<string>(symbols.Count);
                int idx = 0;
                while (idx < symbols.Count)
                {
                    if (idx < symbols.Count - 1
                        && symbols[idx] == bestPair.a
                        && symbols[idx + 1] == bestPair.b)
                    {
                        next.Add(bestPair.a + bestPair.b);
                        idx += 2;
                    }
                    else
                    {
                        next.Add(symbols[idx]);
                        idx++;
                    }
                }
                symbols = next;
            }

            return symbols;
        }

        // --- Parsers ---

        private static Dictionary<string, int> ParseVocabJson(string json)
        {
            // Minimal JSON object parser tuned to CLIP vocab.json layout
            // (flat string→int map). We parse manually so the simulation
            // asmdef does not need to take System.Text.Json as a managed
            // dependency.
            var dict = new Dictionary<string, int>(64000);
            int i = 0;
            while (i < json.Length && json[i] != '{') i++;
            if (i >= json.Length) return dict;
            i++;

            while (i < json.Length)
            {
                while (i < json.Length && (char.IsWhiteSpace(json[i]) || json[i] == ',')) i++;
                if (i >= json.Length || json[i] == '}') break;
                if (json[i] != '"') { i++; continue; }

                i++; // past opening quote
                var key = new StringBuilder();
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        char esc = json[i + 1];
                        if (esc == 'u' && i + 5 < json.Length)
                        {
                            string hex = json.Substring(i + 2, 4);
                            int code = Convert.ToInt32(hex, 16);
                            key.Append((char)code);
                            i += 6;
                            continue;
                        }
                        switch (esc)
                        {
                            case '"': key.Append('"'); break;
                            case '\\': key.Append('\\'); break;
                            case '/': key.Append('/'); break;
                            case 'n': key.Append('\n'); break;
                            case 't': key.Append('\t'); break;
                            case 'r': key.Append('\r'); break;
                            case 'b': key.Append('\b'); break;
                            case 'f': key.Append('\f'); break;
                            default: key.Append(esc); break;
                        }
                        i += 2;
                        continue;
                    }
                    key.Append(json[i]);
                    i++;
                }
                if (i < json.Length) i++; // past closing quote

                while (i < json.Length && (char.IsWhiteSpace(json[i]) || json[i] == ':')) i++;

                int numStart = i;
                while (i < json.Length && (char.IsDigit(json[i]) || json[i] == '-')) i++;
                if (i == numStart) break;

                if (int.TryParse(json.Substring(numStart, i - numStart), out int val))
                    dict[key.ToString()] = val;
            }

            return dict;
        }

        private static Dictionary<(string, string), int> ParseMergesTxt(string[] lines)
        {
            var ranks = new Dictionary<(string, string), int>(48000);
            int rank = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (i == 0 && line.StartsWith("#")) continue; // skip the "#version: ..." header
                if (line.Length == 0) continue;
                int sp = line.IndexOf(' ');
                if (sp < 0) continue;
                string a = line.Substring(0, sp);
                string b = line.Substring(sp + 1);
                ranks[(a, b)] = rank++;
            }
            return ranks;
        }

        private static Dictionary<byte, char> BuildBytesToUnicode()
        {
            // Mirrors openai/CLIP bytes_to_unicode(). Each of the 256 byte
            // values is mapped to a distinct unicode character that BPE merges
            // can operate on without colliding with control characters.
            var bs = new List<int>();
            for (int i = '!'; i <= '~'; i++) bs.Add(i);
            for (int i = 0xA1; i <= 0xAC; i++) bs.Add(i);
            for (int i = 0xAE; i <= 0xFF; i++) bs.Add(i);

            var cs = new List<int>(bs);
            int n = 0;
            for (int b = 0; b < 256; b++)
            {
                if (!bs.Contains(b))
                {
                    bs.Add(b);
                    cs.Add(256 + n);
                    n++;
                }
            }

            var map = new Dictionary<byte, char>(256);
            for (int i = 0; i < bs.Count; i++) map[(byte)bs[i]] = (char)cs[i];
            return map;
        }
    }
}
#endif
