# 16-tts-speech

## HLD - Ne ve Neden (5-10 cumle)

NPC'lerin ekranda **konuşurken** aynı anda kulağa da girmesi için Presentation-only bir TTS zinciri.
Girdi: DialogAdapter'ın streaming callback'inden gelen partial/final metin (`SpeechDirector.FeedPartial/FeedFinal`)
ve `DomainSimulationAdapter.Clock` üzerinden gelen NPC-NPC "talk" olayları (`AmbientVoiceDirector.Offer`).
Çıktı: nöral öncelikli (piper.exe + LibriTTS-R medium, 904 hoparlör) veya SAPI yedekli (Windows COM) ses.
Backend seçimi tek noktada (`SpeechDirector.SpeakRouted`): piper mevcutsa `TrySpeak`+`SpeechPlaybackHost.Enqueue`, aksi halde `WindowsSpeechService.SpeakChunk`.
Her NPC'ye deterministik bir **NpcVoiceSignature** (voice index + rate + pitch) `NpcVoiceSignatureService` üretiyor — kimlik = ses, oturumlar arası kaymıyor.
Konuşan NPC'nin sesi W31'den beri **konumsal**: `SetSpeakerAnchor` verilen `Transform`'a `SpeechPlaybackHost` her frame kilitleniyor (spatialBlend=1); oyuncu/oracle/anlatım 2D kalıyor.
Yakındaki NPC-NPC muhabbetleri kendi hostu (`AmbientVoiceHost`) üzerinden **piper-only** mırıldanıyor — SAPI'nin tek COM kuyruğu dialoğa ait, çakışmıyor.
W36'da (B16) her iki backend **bounded retry + cooldown**'a taşındı: tek bir hiccup oturumu susturmuyor, sadece 3 hata + 30 s cooldown yapıyor; SAPI'nin ProgID = null "gerçekten yok" durumu ise kalıcı (`_sapiMissing`).
`Application.streamingAssetsPath/Models/tts/piper/piper.exe`+`en_US-libritts_r-medium.onnx` **shipped değilse** `Available=false` sessizce SAPI'ye düşüyor — sert bağımlılık yok.

## HLD - Akış (numaralı adımlar)

1. UI/adapter, bir NPC yanıtı için `SpeechDirector.SetSpeakerAnchor(voiceKey, npcBillboard)` çağırıp anchor'ı ekliyor (dialog paneli açılırken).
2. LLM stream'i her tick partial metin üretiyor; `InGameUiController.Update` bunu `SpeechDirector.FeedPartial(voiceKey, displayLine)`'a veriyor.
3. `FeedPartial` display "…" son ekini kırpıyor, "Thinking…"/"… thinks…"/`"..."` placeholder'larını atıyor, `RetargetIfNeeded` ile speaker değişmişse cursor'u sıfırlıyor.
4. `SpeechSentenceChunker.Drain(text, ref _spokenChars)` şu ana kadar bitmiş cümleleri (`.`, `!`, `?` ile biten) çıkarıyor; oluşmakta olan kuyruk parçası bekliyor.
5. Her bitmiş cümle `SpeakRouted(text, voiceKey, purgeFirst:false)` — kuyruğa ekleniyor, aynı konuşmacıda kesme yok.
6. `SpeakRouted`:
   - `PiperSpeechSynth.Available` ise `NpcVoiceSignatureService.SignatureFor(voiceKey, NumSpeakers)` → `TrySpeak(text, VoiceIndex, out wavPath)` → `SpeechPlaybackHost.Enqueue(wavPath, 1f + pitchOffset*0.015f, voiceKey==_anchorKey ? _anchorTransform : null)`.
   - Değilse `WindowsSpeechService.SpeakChunk(text, sig, purgeFirst)` — SAPI XML `<pitch absmiddle=...>`, Rate=1+RateOffset, roster'dan `pick`, async flag 1|8 (+2 purge).
7. `TrySpeak` piper.exe stdin'ine tek JSON satırı `{text, speaker_id, output_file}` yazıyor; piper WAV'ı `Application.temporaryCachePath/tts-out/utt_NNNNN.wav`'a yazıp bitiriyor.
8. `SpeechPlaybackHost.Update` her 100 ms `TryLoadFinishedWav`'ı **EXCLUSIVE** dosya açımıyla deniyor — piper yazımı sürüyorsa `IOException` alınıp bir sonraki poll'e bırakılıyor (yarım klip yok).
9. WAV yüklendiğinde `_source.spatialBlend = anchor != null ? 1f : 0f`, `_source.pitch` set, `transform.position = anchor.position`, `Play()` — voice speaker'la yürüyor.
10. Stream sonlanınca `FeedFinal(voiceKey, finalLine)` kalan kuyruğu (henüz terminatörsüz clause) konuşturuyor; sanitizer düzeltirse ve `_spokenChars > finalLine.Length` ise cursor sıfırlanıp temiz başlanıyor.
11. Farewell / Ctrl-close / ekran kapatma: `StopConversationSpeech()` — piper kuyruğu (`SpeechPlaybackHost.Flush`) + SAPI purge (`WindowsSpeechService.StopSpeaking`) + tüm stream state reset.
12. Paralel patika: `DomainSimulationAdapter.Clock` bir NPC-talk event'ini `AmbientVoiceDirector.Offer(subject, RumorMillSystem.PickFor(...))` ile forwardluyor; guards: piper mevcut + `AnyScreenOpen==false` + 30 s cooldown + host boşta + `ActorView.DomainActorId==subject` bulundu + `Camera.main`'e mesafe ≤ 18 f. Geçerse `TrySpeak`+`AmbientVoiceHost.Play(spatialBlend=1, volume=0.75)`.
13. B16 sözleşmesi: `TrySpeak`/`SpeakChunk` hata verirse `NoteFailure` sayaç 3'e ulaştığında 30 s `IsSilenced()` true; herhangi bir başarı `NoteSuccess` ile sayaç sıfırlanıyor.

## LLD - Veri Modeli (file:line)

- `PiperSpeechSynth` **static**, `Assets/Scripts/Presentation/Ember/Audio/PiperSpeechSynth.cs:12`
  - `const string VoiceFile = "en_US-libritts_r-medium.onnx"` @ `:14`
  - `_proc: System.Diagnostics.Process` @ `:15`, `_stdin: StreamWriter` @ `:16`, `_outDir: string` @ `:17`, `_seq: int` @ `:18`, `_numSpeakers: int` @ `:19`
  - **B16 sayaçlar**: `_failCount: int` `:22`, `_cooldownUntilRealtime: float` `:23`, `MAX_FAILS = 3` `:24`, `COOLDOWN_SECONDS = 30f` `:25`
  - `_probed: bool` `:29`, `_piperDir: string` `:30`
- `SpeechPlaybackHost : MonoBehaviour` (aynı dosyada, `:158`)
  - `s_instance` `:160`, `_queue: Queue<(string path, float pitch, Transform anchor)>` `:161`
  - `_source: AudioSource` `:163`, `_nextPoll: float` `:164`, `_currentAnchor: Transform` `:165`
- `WindowsSpeechService` **static**, `WindowsSpeechService.cs:13`
  - `_voice: object` `:15`, `_roster: object[]` `:16`
  - **B16 sayaçlar**: `_failCount` `:19`, `_cooldownUntilRealtime` `:20`, `MAX_FAILS = 3` `:21`, `COOLDOWN_SECONDS = 30f` `:22`
  - `_sapiMissing: bool` `:23` — **kalıcı**, sadece ProgID=null yolunda `true` işaretleniyor (`:88`)
  - `_last: string` `:27` (legacy `Speak(line)` yolunun de-dup filtresi)
- `SpeechDirector` **static**, `SpeechDirector.cs:12`
  - `_currentKey: ulong` `:14`, `_spokenChars: int` `:15`, `_lastFinal: string` `:16`, `_streamPrefix: string` `:17`
  - **W31 spatial**: `_anchorKey: ulong` `:18`, `_anchorTransform: Transform` `:19`
- `AmbientVoiceDirector` **static**, `AmbientVoiceDirector.cs:11`
  - `s_host: AmbientVoiceHost` `:13`, `s_nextOfferTime: float` `:14`
- `AmbientVoiceHost : MonoBehaviour` (aynı dosyada, `:52`)
  - `_source: AudioSource` `:54`, `_anchor: Transform` `:55`, `_pendingWav: string` `:56`, `_pendingPitch: float` `:57`
- Yardımcı model (namespace `EmberCrpg.Simulation.AiDm`):
  - `NpcVoiceSignature { VoiceIndex, RateOffset, PitchOffset }` — `Assets/Scripts/Simulation/AiDm/NpcVoiceSignatureService.cs:6`
  - `NpcVoiceSignatureService` `:22` — SplitMix64 hash; VoiceIndex ∈ [0, availableVoices), Rate ∈ [-3,+3], Pitch ∈ [-9,+9]
  - `SpeechSentenceChunker.Drain(text, ref fromIndex): List<string>` `:52`

## LLD - Fonksiyon Haritası (imza + file:line + 1 cümle)

**PiperSpeechSynth.cs**
- `bool Available { get; }` `:34` — bir kez `_probed`, streamingAssets'te piper.exe + voice .onnx varsa `_piperDir` set, num_speakers okunur; silenced'ken `false`.
- `int NumSpeakers => _numSpeakers > 0 ? _numSpeakers : 1;` `:60`
- `bool TrySpeak(string text, int speakerId, out string wavPath)` `:62` — `EnsureProcess`; JSON tek satır stdin'e; başarıda `NoteSuccess`, hatada child kill + `NoteFailure` + SAPI'ye fallback için `false`.
- `void EnsureProcess()` `:92` — `_outDir` altında eski `utt_*.wav`'ları sil, piper.exe'yi `--model ... --json-input` ile başlat, iki pipe'ı da drain et, `Application.quitting += Kill`.
- `void Kill()` `:123`, `int ReadNumSpeakers(string)` `:129`, `string JsonEscape(string)` `:145`
- `bool IsSilenced()` `:26`, `void NoteFailure()` `:27`, `void NoteSuccess()` `:28` — B16 üçlüsü.

**SpeechPlaybackHost (aynı dosya)**
- `static void Enqueue(string wavPath, float pitch, Transform anchor = null)` `:167` — singleton spawn + kuyruğa ekle.
- `static void Flush()` `:173` — kuyruğu boşalt + `_source.Stop()` (W31 farewell yolu buraya varır).
- `static void Ensure()` `:180` — `DontDestroyOnLoad` host + tek AudioSource (Linear rolloff 2..18f, doppler 0).
- `void Update()` `:192` — anchor varsa `transform.position` speaker'a kilitli, 100 ms poll'lu `TryLoadFinishedWav`, klip hazır olduğunda `spatialBlend` anchor'a göre 1/0, sonra `Play()`.
- `static AudioClip TryLoadFinishedWav(string path)` `:216` — `FileShare.None` exclusive open, `IOException` → yazım sürüyor, `null` dön.
- `static AudioClip TryLoadFinishedWavPublic(string path)` `:214` — `AmbientVoiceHost`'un okuduğu ince proxy (internal).
- `static AudioClip ParsePcm16Wav(byte[] wav, string name)` `:236` — 'data' chunk'ı tara, PCM16→float[], `AudioClip.SetData`.

**WindowsSpeechService.cs**
- `int VoiceCount { get; }` `:29` — `EnsureVoice`; roster.Length veya 1.
- `void Speak(string line)` `:32` — legacy tek-satır, `_last == line` ise no-op, purgeFirst=true.
- `void SpeakChunk(string text, NpcVoiceSignature signature, bool purgeFirst)` `:39` — roster'dan `VoiceIndex` mod ile pick, Rate set, 300 char kırpma, SAPI XML `<pitch absmiddle=...>`, async flag `1|8|(2 if purgeFirst)`.
- `void StopSpeaking()` `:71` — boş `Speak(string.Empty, 1|2)` = purge queue.
- `void EnsureVoice()` `:84` — SAPI.SpVoice COM, roster çekiliyor; ProgID=null → `_sapiMissing=true` (kalıcı); catch B16 sayacı.
- `IsSilenced/NoteFailure/NoteSuccess` `:24-26` — B16 üçlüsü.

**SpeechDirector.cs**
- `void SetSpeakerAnchor(ulong voiceKey, Transform anchor)` `:25` — W31: sadece bu key'in klipleri 3D olacak.
- `void StopConversationSpeech()` `:34` — W31 farewell: state reset + `SpeechPlaybackHost.Flush()` + `WindowsSpeechService.StopSpeaking()`.
- `void FeedPartial(ulong voiceKey, string displayLine)` `:42` — placeholder ele, `RetargetIfNeeded`, "shrink or diverge = new stream" tespiti, `SpeechSentenceChunker.Drain` → `SpeakRouted`.
- `void FeedFinal(ulong voiceKey, string finalLine)` `:61` — dedup `_lastFinal`, kalan clause'ı konuştur, cursor tamamı olarak işaretle.
- `void SpeakRouted(string text, ulong voiceKey, bool purgeFirst)` `:79` — piper-first + SAPI fallback; purgeFirst piper yolunda `Flush()`.
- `NpcVoiceSignature SignatureFor(ulong voiceKey)` `:96` — SAPI için VoiceCount ile signature çevir.
- `void RetargetIfNeeded(ulong voiceKey)` `:100` — speaker değişince cursor sıfırla ama **kuyruğu tutmaya** özen göster.
- `string StripDisplaySuffix(string)` `:110`, `bool IsPlaceholder(string)` `:113`.

**AmbientVoiceDirector.cs**
- `void Offer(ulong actorId, string line)` `:16` — W31 spatial mutter: 6 guard (piper, cooldown, `InGameUiController.AnyScreenOpen`, host boşta, `ActorView` bulundu, ≤18 f).
- `void Ensure()` `:43` — `DontDestroyOnLoad("AmbientVoiceHost")`.
- `AmbientVoiceHost.Busy` `:58`, `void Play(string wavPath, float pitch, Transform anchor)` `:60`.
- `void EnsureSource()` `:66` — spatialBlend=1, Linear 2..18 f, volume 0.75.
- `void Update()` `:78` — anchor'a kilit + WAV hazır olduğunda tek klipi çal.

## LLD - Yazdığı/Okuduğu Alanlar (FieldOwnershipRegistry dilinde)

Bu sistemin **Domain/WorldState'e yazdığı hiçbir alan yok** — tamamı Presentation-katman static/instance state'i. FieldOwnership perspektifi:

| Alan | Sahip | Yazan | Okuyan |
| --- | --- | --- | --- |
| `PiperSpeechSynth._proc/_stdin/_outDir/_seq/_probed/_piperDir/_numSpeakers` | `PiperSpeechSynth` | `EnsureProcess`, `Available` probe | `TrySpeak`, `Available`, `NumSpeakers` |
| `PiperSpeechSynth._failCount/_cooldownUntilRealtime` | `PiperSpeechSynth` | `NoteFailure`/`NoteSuccess` | `IsSilenced`, `Available` |
| `SpeechPlaybackHost._queue/_source/_nextPoll/_currentAnchor` | `SpeechPlaybackHost` | `Enqueue`, `Flush`, `Update` | `Update` |
| `WindowsSpeechService._voice/_roster/_last` | `WindowsSpeechService` | `EnsureVoice`, `Speak`, catch bloğu | `SpeakChunk`, `StopSpeaking`, `VoiceCount` |
| `WindowsSpeechService._failCount/_cooldownUntilRealtime/_sapiMissing` | `WindowsSpeechService` | `NoteFailure`/`NoteSuccess`, `EnsureVoice` (ProgID=null) | `SpeakChunk`, `StopSpeaking`, `EnsureVoice`, `Speak` |
| `SpeechDirector._currentKey/_spokenChars/_lastFinal/_streamPrefix` | `SpeechDirector` | `RetargetIfNeeded`, `FeedPartial`, `FeedFinal`, `StopConversationSpeech` | `FeedPartial`, `FeedFinal` |
| `SpeechDirector._anchorKey/_anchorTransform` | `SpeechDirector` | `SetSpeakerAnchor`, `StopConversationSpeech` | `SpeakRouted` (piper Enqueue anchor seçimi) |
| `AmbientVoiceDirector.s_host/s_nextOfferTime` | `AmbientVoiceDirector` | `Ensure`, `Offer` (cooldown) | `Offer` |
| `AmbientVoiceHost._source/_anchor/_pendingWav/_pendingPitch` | `AmbientVoiceHost` | `EnsureSource`, `Play`, `Update` | `Busy`, `Update` |

Okunan dışsal state:
- `Application.streamingAssetsPath`, `Application.temporaryCachePath`, `Application.quitting`
- `Time.realtimeSinceStartup` (B16 cooldown), `Time.unscaledTime` (Enqueue poll + Offer cooldown)
- `Camera.main.transform.position` (AmbientVoiceDirector.Offer earshot check)
- `ActorView.DomainActorId`, `ActorView.transform` (Offer'da anchor arama, `FindObjectsByType<ActorView>`)
- `InGameUiController.AnyScreenOpen` (Offer'da modal susturucu)
- `NpcVoiceSignatureService.SignatureFor(voiceKey, numSpeakers)` (deterministik ses kimliği)
- `SpeechSentenceChunker.Drain(text, ref cursor)` (saf splitter)

## LLD - Ürettiği/Tükettiği Olaylar

**Tüketilen (upstream olaylar → bu sisteme çağrı):**
- LLM streaming callback → `SpeechDirector.FeedPartial/FeedFinal`
  - `InGameUiController.cs:179,181` (dialog panel)
  - `InGameUiController.cs:123` (oracle 42UL yolu)
  - `ConsulFateView.cs:151` (oracle 7UL)
  - `DomainSimulationAdapter.Dialog.Source.cs:149`
- Dialog panel yaşam döngüsü → `SpeechDirector.SetSpeakerAnchor` / `StopConversationSpeech`
  - `InGameUiController.cs:486` (ekran kapanma), `:512` (dialog başlama)
  - `EmberWorldHost.Input.cs:27` (Ctrl-close global cut)
- Sim'in NPC-talk event akışı → `AmbientVoiceDirector.Offer`
  - `DomainSimulationAdapter.Clock.cs:77` (RumorMillSystem.PickFor ile line üretiliyor)
- Unity yaşam döngüsü → `Application.quitting += PiperSpeechSynth.Kill` (`PiperSpeechSynth.cs:121`)

**Üretilen (downstream tarafta gözlemlenen):**
- **Hiçbir Domain/Sim event'i emit edilmiyor** — çıktı sadece işitsel + `Debug.Log` iz kayıtları:
  - `"[Piper] voice found — {n} speakers on the roster."`
  - `"[Piper] no voice model shipped — SAPI backend stays on duty."`
  - `"[Piper] process up — neural voices online."`
  - `"[Piper] synth failed ({n}/{max}), falling back to SAPI: {e}"`
  - `"[Piper] bad wav {path}: {e}"`
  - `"[Speech] SAPI roster: {n} voice(s) — signatures map across them."`
  - `"[Speech] SAPI hiccup ({n}/{max}), staying silent briefly: {e}"`
  - `"[Speech] SAPI init hiccup ({n}/{max}), staying silent briefly: {e}"`
- Yan etki: `piper.exe` child process (RedirectStandardInput/Output/Error, CreateNoWindow), `Application.temporaryCachePath/tts-out/utt_NNNNN.wav` dosyaları (sonraki `EnsureProcess`'te silinir).

## Testler (bu sistemi pinleyen test dosyaları — W32-W36 hikaye-testleri dahil)

- `Assets/Tests/EditMode/Presentation/SpeechRetryCooldownTests.cs` — **W36 / B16 pin (yeni)**
  - `Piper_SingleTransientFailure_DoesNotSilenceTheSession`
  - `Piper_ThreeFailuresWithinWindow_SilencesUntilCooldown`
  - `Piper_NoteSuccess_ResetsCounter`
  - `Windows_ThreeFailuresSilenceButFourthAfterCooldownReturns`
  - `Windows_SapiMissing_StaysPermanentAcrossAnyCooldown`
  - `Windows_NoteSuccess_ResetsCounter`
  - Reflection ile `_failCount/_cooldownUntilRealtime/_sapiMissing` alanlarına + `IsSilenced/NoteFailure/NoteSuccess` metotlarına erişiyor.
- `Assets/Tests/EditMode/AiDm/NpcVoiceSignatureServiceTests.cs` — M3b pin
  - `SignatureFor_SameKey_IsStable_AndInRange`
  - `SignatureFor_SpreadsAcrossVoicesAndPitches`
  - `Chunker_DrainsCompleteSentences_KeepsTheFormingTail` (SpeechSentenceChunker)
- `Assets/Tests/EditMode/AiDm/PlayerVoiceServiceTests.cs` — M3b.3 pin
  - `PlayerVoiceKey_IsStable`
  - `PlayerVoiceKey_ClassChangesTheVoice`
  - `PlayerVoiceKey_NeverZero_EvenOnEmptyInputs`
- `Assets/Tests/EditMode/AiDm/DialogStreamTextTests.cs` — komşu sistem (dialog splitter), ama bu boru hattının **girdisini** koruyor (parrot metinler `SpeechDirector`'a hiç ulaşmasın diye).

**Pinsiz alanlar (borç):** `SpeechDirector.FeedPartial` state-makinesi (retarget/reset/prefix), `SpeakRouted` neural→SAPI fallback zinciri, `AmbientVoiceDirector.Offer` 6-guard, `SpeechPlaybackHost.TryLoadFinishedWav` exclusive-open retry — hiçbirinin pin'li testi yok.

## W32-W36 Değişiklikleri (bu sistemin son 5 haftadaki büyük hareketleri)

- **W31** (`1e9b474b`, 2026-07-24) — sekiz canlı yaranın üçüncüsü bu sistemi biçimlendirdi:
  - `SpeechDirector.SetSpeakerAnchor` + `_anchorKey/_anchorTransform` eklendi; klipler artık speaker Transform'a kilitli (spatialBlend=1).
  - `SpeechDirector.StopConversationSpeech` eklendi (daha önce `StopSpeaking`'in **sıfır çağrısı** vardı) — Ctrl-close ve `InGameUiController.CloseScreen` bu yola bağlandı.
  - `AmbientVoiceDirector` + `AmbientVoiceHost` **yeni**: NPC-NPC "talk" event'lerine spatial ambient mutter, piper-only, budget 1, 30 s cooldown, 18 f earshot.
  - `SpeechPlaybackHost` içine internal `TryLoadFinishedWavPublic` proxy'si (ambient host'un aynı WAV parser'ı kullanabilmesi için).
- **W32** (`5049d445`, `c477c217`) — EAT slice + hotfixler; **bu sisteme dokunmadı** (sim tarafı).
- **W33** (`61e340f3`) — FARM slice; **bu sisteme dokunmadı**.
- **W34** (`3aa87cf6`, `9012485e`) — SLEEP + WORK slice; **bu sisteme dokunmadı**.
- **W35** (`20a3b899`) — ScheduleSystem küçültme + ownership; **bu sisteme dokunmadı**.
- **W36** (`f6c9e2d0`, 2026-07-25) — **RUH_TESHIS tail**, B16 kalıcı çözüm:
  - `PiperSpeechSynth`'te `_dead` tek-yönlü kilit silindi → `_failCount/_cooldownUntilRealtime/MAX_FAILS=3/COOLDOWN_SECONDS=30` + `IsSilenced/NoteFailure/NoteSuccess` üçlüsü.
  - `WindowsSpeechService`'te aynı üçlü + tek istisna: ProgID=null olduğunda `_sapiMissing=true` kalıcı (COM hiç kurulu değil senaryosu).
  - `EnsureVoice` ve `StopSpeaking` son `_dead` referansları temizlendi.
  - Pin: `SpeechRetryCooldownTests` (6 hikaye-testi, reflection ile private static'lara).

## Bilinen Borçlar + Kaçak Kapıları

**Borçlar:**
- **Test kapsamı**: yalnız retry/cooldown ve signature/chunker pinli. `SpeechDirector` cursor state-makinesi (özellikle "shrink veya diverge = new stream" heuristik'i), `SpeakRouted` piper→SAPI fallback, `AmbientVoiceDirector.Offer` altı-guard'ı — hepsi pinsiz; regression yakalayacak test yok.
- **Reflection'a bağımlı testler**: `SpeechRetryCooldownTests` `_failCount/_cooldownUntilRealtime/_sapiMissing` private field adlarına + `IsSilenced/NoteFailure/NoteSuccess` metod adlarına kilitli — bir rename testleri sessizce kırar (compile ederler ama davranışı doğrulamazlar? Aslında `GetField`/`GetMethod` null döner + `NullReferenceException` — o yüzden kırılır, ama hata mesajı yanıltıcıdır).
- **Sihirli sabitler her yerde**: `18f` (earshot ve rolloff maxDistance) 3 yerde, `2f` (minDistance) 2 yerde, `30f` (Offer cooldown ile backend cooldown), `0.10f` (playback poll), `0.75f` (ambient volume), `300` (SAPI clip), `0.015f` (pitchOffset→AudioSource.pitch skalası) — tek config noktası yok.
- **`_last` legacy dedup**: `WindowsSpeechService._last` sadece `Speak(string)` legacy giriş noktasında; `SpeakChunk` yolunda dedup yok — aynı chunk çift gelirse çift söylenir.
- **`FindObjectsByType<ActorView>` her `Offer`'da**: town-scale'de her mırıldanmada tüm sahne taranır (O(n)). ID→Transform sözlüğü yok.
- **SAPI 300-char kırpma**: `SpeakChunk`'ta uzun bir cümlenin ortasında kesilebiliyor (chunker doğrusal olarak paragraf uzunluğunu kısıtlamıyor).

**Kaçak kapıları:**
- `_sapiMissing=true` **oturum içinde sıfırlanmıyor**: kullanıcı SAPI'yi kurup restart etmezse aynı çalışan SAPI'ye asla dönmez.
- `PiperSpeechSynth._probed=true` bir kez set olduktan sonra dosya sistemi değişse bile yeniden probe yok; oyun sırasında model dosyası eklenirse görmez.
- `SpeechPlaybackHost` singleton `DontDestroyOnLoad`; sahne geçişlerinde kuyrukta bekleyen stale WAV path'leri kalabilir (`Flush` explicit çağrılmazsa).
- `Application.temporaryCachePath/tts-out` altındaki eski WAV'lar sadece `EnsureProcess` içinde temizleniyor; piper zaten canlıysa (idempotent early return) temizlik olmaz — uzun oturumda disk sürünmesi.
- Piper child process'ini `Kill` yalnız `Application.quitting`'e bağlı; Editor **domain reload**'unda static'ler sıfırlanır ama child process **öksüz** yaşamaya devam edebilir (yeni `EnsureProcess` başka bir piper.exe daha fork'lar).
- `AmbientVoiceDirector.Offer` `Camera.main`'e bağımlı — yeni bir "Main" tag'li kamera yoksa mırıltı hiç oynamaz (sessizce return, log yok).
- `SpeechDirector`'un tüm state'i static — Editor test ortamında SetUp'ta manuel sıfırlama gerekir, `Reset` API yok (`StopConversationSpeech` yeterli ama isim başkasını çağrıştırıyor).
- `SpeechPlaybackHost.ParsePcm16Wav` sadece PCM16 destekliyor; piper varsayılan çıktısı bu olduğu için çalışıyor, ama voice modeli değişirse (float32 çıktı) sessiz `AudioClip.Create("empty", 1, 1, 22050, false)` döner.
