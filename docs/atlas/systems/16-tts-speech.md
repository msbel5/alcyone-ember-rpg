# 16-tts-speech

## HLD - Ne ve Neden

Konusan her varlik — NPC, oyuncu, kahin/DM — kalici ve taninabilir bir SESE sahip olsun:
sistemin felsefesi "ses = kimlik"tir ve forge'un portreyi seed'den turetmesi gibi, ses imzasi da
aktor id'sinden deterministik turetilir (`NpcVoiceSignatureService.cs:3-4`). Ikinci amac
AKIS: LLM cevabi hala uretilirken tamamlanan cumleler aninda seslendirilir, "kulak, gozun
okudugu akisi takip eder" (`SpeechDirector.cs:7-9`). Iki backend vardir: birincil olarak Piper
neural TTS (`piper.exe` alt sureci, 904 konusmacili LibriTTS-R modeli, tamamen offline,
`PiperSpeechSynth.cs:8-14`), model dosyalari yoksa veya surec olurse Windows SAPI (COM
reflection ile, paketsiz/agsiz, `WindowsSpeechService.cs:246-252`). SAPI de yoksa sistem
sessizce susar — ses hicbir zaman sert bagimlilik degildir (`WindowsSpeechService.cs:251`,
`PiperSpeechSynth.cs:14`). Oyuncuya gorunen etki: NPC yazarken konusmaya baslar, her NPC
her oturumda ayni sesle konusur, oyuncunun kendi sorulari kendi sesiyle okunur (M3b.3,
`PlayerVoiceService.cs:3-6`) ve kahin (DM) sabit bir kahin sesine sahiptir
(`ConsulFateView.cs:104-106`). Sistem tamamen sunum katmanindadir; simulasyona hicbir sey
yazmaz.

## HLD - Akis

Kadans: sim tick'i DEGIL, sunum frame'i. Tum giris noktalari Unity `Update()` veya UI
callback'idir.

1. **NPC akisi (her frame):** `InGameUiController.Update` acik dialog varken kaynaktan o anki
   satiri ceker; `IsThinking` ise `SpeechDirector.FeedPartial(voiceKey, line)`, degilse
   `FeedFinal(voiceKey, line)` (`InGameUiController.cs:165-173`). `voiceKey` adaptorun
   `VoiceKey` ozelliginden gelir: gercek `ActorId.Value`, yoksa isim uzerinden FNV-1a
   (`DomainSimulationAdapter.Dialog.Source.cs:26-29`).
2. **Oyuncu sesi (tiklama/yazma aninda):** bir konu secildiginde veya serbest soru
   yazildiginda `SpeakPlayerQuestion` soruyu dogal cumleye cevirip
   (`DialogStreamText.NaturalQuestion`, `DialogStreamText.cs:54`) oyuncunun anahtariyla
   `FeedFinal` cagirir (`DomainSimulationAdapter.Dialog.Source.cs:128-137`; cagiranlar:
   `Dialog.Topics.cs:33,87`, "Any news?" yolu `Dialog.Source.cs:302`, kahin sorusu
   `InGameUiController.cs:536`).
3. **Kahin cevabi:** LLM cevabi cozuldugunde sabit anahtar `7UL` ile `FeedFinal`
   (`ConsulFateView.cs:106`). **Headless kanit yuzeyi:** `--ember-speech-check` bayragiyla
   dunya acilisinda `FeedFinal(42UL, ...)` tam yigini bir kez calistirir
   (`InGameUiController.cs:117-124`).
4. **SpeechDirector on-isleme:** " …" gorunum eki soyulur (`SpeechDirector.cs:89-90`),
   "Thinking…"/"X thinks…"/"..." placeholder'lari atlanir (`SpeechDirector.cs:92-93`).
   Konusmaci degisirse offsetler sifirlanir ama kuyruk BOSALTILMAZ — sesler konusma
   sirasina gore kuyruklanir (`SpeechDirector.cs:78-87`, canli hata notu 84-86).
5. **Yeni-akis tespiti (stream prefix):** metin kisalmissa VEYA 12 karakterlik prefix
   cakismiyorsa bu yeni bir cevaptir; `_spokenChars` ve `_streamPrefix` sifirlanir
   (`SpeechDirector.cs:27-34`; "ilk cumleyi seslendirmedi" canli hatasinin duzeltmesi).
6. **Cumle drenaji:** `SpeechSentenceChunker.Drain` `.` `!` `?` sonlandiricili tamam
   cumleleri cikartir, olusan kuyruk basina `SpeakRouted` cagrilir; sonlanmamis kuyruk
   bekler (`SpeechDirector.cs:35-37`, `NpcVoiceSignatureService.cs:52-68`).
7. **Yonlendirme:** Piper varsa imza `NumSpeakers`'a gore hesaplanir, cumle JSON satiri
   olarak piper stdin'ine yazilir ve donen wav yolu pitch ile `SpeechPlaybackHost`'a
   kuyruklanir (`SpeechDirector.cs:58-68`; pitch formulu `1f + PitchOffset * 0.015f`,
   `SpeechDirector.cs:67`). Piper yoksa/olduyse ayni imza matematigiyle SAPI
   `SpeakChunk` (`SpeechDirector.cs:71`).
8. **Calma:** `SpeechPlaybackHost.Update` 0.10 sn'de bir kuyrugun basindaki wav'i EXCLUSIVE
   acmayi dener; piper hala yaziyorsa acilis basarisiz olur ve sonraki poll'da tekrar denenir
   — yarim okunan klip olmaz (`PiperSpeechSynth.cs:178-191,193-213`). PCM16 wav elle parse
   edilip `AudioClip`'e cevrilir (`PiperSpeechSynth.cs:215-238`).
9. **Final kalani:** `FeedFinal` akisin seslendirmedigi kuyrugu okur; final metni
   konusulandan sapmis ise bastan baslar; akista HIC konusulmamis bir final "yeni cevap
   ikamesi" sayilip kuyrugu temizler (`SpeechDirector.cs:40-54`, purge karari 53).

## LLD - Veri Modeli

- **`NpcVoiceSignature`** (readonly struct, `NpcVoiceSignatureService.cs:5-20`):
  `VoiceIndex` (backend rosterina modulo-maplenir, :8), `RateOffset` -3..+3 (:10),
  `PitchOffset` -9..+9 (:12).
- **`SpeechDirector` statik durumu** (`SpeechDirector.cs:16-19`): `_currentKey` (aktif
  konusmaci), `_spokenChars` (akis offseti), `_lastFinal` (final dedupe), `_streamPrefix`
  (12 karlik yeni-akis cipasi). Tek global set — ayni anda tek konusma varsayimi.
- **`PiperSpeechSynth` statik durumu** (`PiperSpeechSynth.cs:18-26`): `VoiceFile` sabiti
  `en_US-libritts_r-medium.onnx` (:18), `_proc`/`_stdin` (canli alt surec), `_outDir`
  (`temporaryCachePath/tts-out`, :89), `_seq` (wav adi sayaci), `_numSpeakers` (model
  json'undan, :41), `_dead` (kalici devre-disi bayragi), `_probed`, `_piperDir`.
- **`SpeechPlaybackHost`** (MonoBehaviour, `PiperSpeechSynth.cs:148-153`):
  `Queue<(string path, float pitch)>` `_queue`, tek `AudioSource` (`spatialBlend=0`,
  "3D voices come with M3b.3" notu :175 — hala 2D), `_nextPoll`. `DontDestroyOnLoad`
  singleton'i lazy kurulur (:168-176).
- **`WindowsSpeechService` statik durumu** (`WindowsSpeechService.cs:16-19`): `_voice`
  (SAPI.SpVoice COM nesnesi, ProgID ile :81), `_roster` (kurulu SAPI sesleri, :84-91),
  `_dead`, `_last` (legacy `Speak` dedupe).
- **Ses anahtarlari (ulong):** NPC = `ActorId.Value` ya da isim FNV-1a'si
  (`Dialog.Source.cs:26-29`); oyuncu = `FNV(name) XOR FNV(class)*0x9E3779B97F4A7C15`
  (`PlayerVoiceService.cs:10-15`; `PlayerClassName` `WorldState.cs:146`'da yasar ve
  `WorldSaveMapper.cs:92,200` ile save'e girer — ses reload'da ayni kalir); kahin = sabit
  `7UL` (`ConsulFateView.cs:106`); speech-check = `42UL` (`InGameUiController.cs:122`).
- **Sevk edilen varliklar (diskte dogrulandi):** `Assets/StreamingAssets/Models/tts/piper/`
  icinde `piper.exe`, model + `.onnx.json`, `espeak-ng.dll` + `espeak-ng-data`, piper'in
  KENDI `onnxruntime.dll`'i — surec izolasyonu sayesinde forge'un onnxruntime kopyasiyla
  asla karsilasmaz (`PiperSpeechSynth.cs:12`).

## LLD - Fonksiyon Haritasi

- `SpeechDirector.FeedPartial(ulong voiceKey, string displayLine)` — `SpeechDirector.cs:21-38`;
  akan satirdan tamam cumleleri drenar, yeni-akis tespitiyle offseti sifirlar.
- `SpeechDirector.FeedFinal(ulong voiceKey, string finalLine)` — `SpeechDirector.cs:40-54`;
  seslendirilmemis kalani konusur, prefix cipasini birakir.
- `SpeechDirector.SpeakRouted(string, ulong, bool purgeFirst)` — `SpeechDirector.cs:58-72`;
  Piper-once yonlendirme, basarisizlikta SAPI'ye ayni imza matematigiyle duser.
- `SpeechDirector.RetargetIfNeeded(ulong)` — `SpeechDirector.cs:78-87`; konusmaci degisiminde
  durum sifirlar, kuyrugu KORUR.
- `NpcVoiceSignatureService.SignatureFor(ulong voiceKey, int availableVoices)` —
  `NpcVoiceSignatureService.cs:26-37`; splitmix64-tarzi avalanche ile deterministik
  voice/rate/pitch (:28-35).
- `NpcVoiceSignatureService.VoiceKeyFor(string actorName)` — `NpcVoiceSignatureService.cs:40-45`;
  FNV-1a, sifir sonucu 1'e katlanir.
- `SpeechSentenceChunker.Drain(string text, ref int fromIndex)` —
  `NpcVoiceSignatureService.cs:52-68`; saf fonksiyon, tamam cumleleri dondurur, kuyruk kalir.
- `PlayerVoiceService.PlayerVoiceKey(string playerName, string className)` —
  `PlayerVoiceService.cs:10-15`; yaratim secimlerinden kalici oyuncu anahtari.
- `PiperSpeechSynth.Available { get; }` — `PiperSpeechSynth.cs:28-54`; tek seferlik dosya
  probe'u; model yoksa log atip false kalir. Yalnizca Windows (`#if` :32).
- `PiperSpeechSynth.TrySpeak(string text, int speakerId, out string wavPath)` —
  `PiperSpeechSynth.cs:58-83`; JSON satirini stdin'e yazar, wav yolunu geri verir; istisna
  `_dead=true` yapar (kalici SAPI'ye dusus).
- `PiperSpeechSynth.EnsureProcess()` — `PiperSpeechSynth.cs:86-114`; alt sureci baslatir,
  eski wav'lari temizler (:91-92), stdout/stderr'i bosaltir (:107-111, aksi halde piper
  tikanir), `Application.quitting`'e `Kill` baglar (:112).
- `PiperSpeechSynth.ReadNumSpeakers(string configPath)` — `PiperSpeechSynth.cs:122-136`;
  model json'undan `num_speakers`'i elle substring-parse eder (JSON kutuphanesiz).
- `SpeechPlaybackHost.Enqueue(string wavPath, float pitch)` / `Flush()` —
  `PiperSpeechSynth.cs:155-159,161-166`; kuyruga ekle / kuyrugu bosalt + calani durdur.
- `SpeechPlaybackHost.Update()` — `PiperSpeechSynth.cs:178-191`; 0.10 sn poll, sirali calma.
- `SpeechPlaybackHost.TryLoadFinishedWav(string)` — `PiperSpeechSynth.cs:193-213`;
  `FileShare.None` ile exclusive acilis kapisi (:199); bozuk wav'da 1-sample bos klip (:211).
- `SpeechPlaybackHost.ParsePcm16Wav(byte[], string)` — `PiperSpeechSynth.cs:215-238`;
  'data' chunk taramali elle PCM16 parse.
- `WindowsSpeechService.SpeakChunk(string, NpcVoiceSignature, bool purgeFirst)` —
  `WindowsSpeechService.cs:31-59`; COM reflection ile Voice sec (:42), Rate ayarla (:45-46),
  300 karakter kirp (:47), pitch'i SAPI XML olarak dokur (:48), bayraklar `1|8|(purge?2:0)`
  (:50).
- `WindowsSpeechService.Speak(string line)` — `WindowsSpeechService.cs:24-29`; legacy tek
  satir girisi, varsayilan imza. **Cagirani bulunamadi** (repo grep'i yalnizca tanimi buldu)
  — olu-adaya benziyor, dogrulanmadi (reflection/gelecek kullanim olabilir).
- `WindowsSpeechService.StopSpeaking()` — `WindowsSpeechService.cs:61-73`; bos metin +
  purge bayragiyla en ucuz SAPI kuyruk temizligi. **Cagirani bulunamadi.**
- `WindowsSpeechService.EnsureVoice()` — `WindowsSpeechService.cs:75-100`; `SAPI.SpVoice`
  ProgID'sinden COM nesnesi + kurulu ses rosteri; hatada `_dead=true`, sessiz kalir.
- `DomainSimulationAdapter.VoiceKey { get; }` — `Dialog.Source.cs:26-29`; id-oncelikli anahtar.
- `DomainSimulationAdapter.SpeakPlayerQuestion(string)` — `Dialog.Source.cs:128-137`;
  oyuncunun sorusunu oyuncunun sesiyle final olarak besler.

## LLD - Yazdigi/Okudugu Alanlar

`FieldOwnershipRegistry` (`Assets/Scripts/Simulation/Composition/FieldOwnershipRegistry.cs:12-54`)
bu sisteme ait HICBIR kayit icermez — TTS hicbir sim alanina yazmaz; tamamen sunum
katmanidir ve tek-yazar defterinin disindadir (dogru konum: defter yalnizca mutable
aktor/dunya alanlarini kapsar).

Okuduklari:
- `WorldState.PlayerClassName` (`WorldState.cs:146`; `Dialog.Source.cs:135` uzerinden) ve
  oyuncu `ActorRecord.Name` (`CompanionService.FindPlayer`, `Dialog.Source.cs:132`).
- `IDialogSource.GetCurrentLine()/IsThinking/VoiceKey` (frame basi,
  `InGameUiController.cs:165-173`).
- Disk: `StreamingAssets/Models/tts/piper/*` (probe, `PiperSpeechSynth.cs:37-41`),
  `temporaryCachePath/tts-out/utt_*.wav` (okuma, :193-205).

Yazdiklari (defter-disi, sunum/OS yan etkileri):
- OS alt sureci `piper.exe` (`PiperSpeechSynth.cs:105`) ve stdin'i (:70-71).
- `temporaryCachePath/tts-out/utt_NNNNN.wav` dosyalari (piper yazar, adres :67; boot
  temizligi :91-92).
- `DontDestroyOnLoad` "SpeechPlaybackHost" GameObject'i + `AudioSource.pitch/clip/Play`
  (`PiperSpeechSynth.cs:171-175,188-190`).
- SAPI COM `Voice`/`Rate` ozellikleri ve `Speak` kuyrugu (`WindowsSpeechService.cs:42-51`).

## LLD - Urettigi/Tukettigi Olaylar

- **WorldEventKind: uretmez.** Yakin komsu: konu secimi `WorldEventKind.ActorTalked`
  ("topic_selected id:...") uretir ama bu dialog sisteminin isidir, TTS'in degil
  (`Dialog.Source.cs:332-337`).
- **Log taglari:** `[Piper]` — roster bulundu (`PiperSpeechSynth.cs:42`), model yok (:46),
  synth hatasi/SAPI'ye dusus (:77), surec ayakta (:113), bozuk wav (:210). `[Speech]` —
  SAPI kullanilamiyor (`WindowsSpeechService.cs:56,97`), roster sayisi (:92).
- **Tukettigi:** `Application.quitting` (piper'i oldurmek icin, `PiperSpeechSynth.cs:112`);
  `System.Environment.CommandLine` icindeki `--ember-speech-check` bayragi
  (`InGameUiController.cs:119`).

## Testler

- `Assets/Tests/EditMode/AiDm/NpcVoiceSignatureServiceTests.cs` — imza kararliligi + aralik
  pini (:10-21), 60 NPC'de ses/pitch sacilimi (:24-36), chunker'in tamam-cumle drenaji ve
  kuyruk korumasi (:39-47).
- `Assets/Tests/EditMode/AiDm/PlayerVoiceServiceTests.cs` — oyuncu anahtari kararli (:9-12),
  sinif degisince ses degisir (:14-17), bos girdide asla sifir degil (:19-21).
- **Test disi kalanlar (gercek durum):** `SpeechDirector`'un akis durum makinesi (offset
  sifirlama, prefix cipasi, purge karari), `PiperSpeechSynth` surec yasam dongusu,
  `SpeechPlaybackHost` wav parse/exclusive-open kapisi ve `WindowsSpeechService` COM yolu
  hicbir birim testiyle pinli DEGIL (Unity/surec/COM bagimli). Tek calisir-durum kaniti
  `--ember-speech-check` bayragidir (`InGameUiController.cs:117-124`); yorum "headless runs
  can assert wavs" der ama repo icinde bu bayragi calistirip wav varligini dogrulayan bir
  betik BULUNAMADI (dogrulanmadi — CI disi elle kosuluyor olabilir).

## Bilinen Borclar + Kacak Kapilari

Aile harfleri `docs/SYSTEMS_ATLAS.md:52-60` taksonomisine gore. Bu sistem (f) ailesinin
("akislarda offset/durum-sifirlama hatalari — TTS ilk-cumle") ADIYLA anilan uyesidir.

1. **(f) Akis-offset durum makinesi hala kirilgan ve testsiz.** Iki canli hata dogrudan bu
   dosyada yamandi: ayni konusmaciya ikinci soruda eski offset yuzunden ilk cumlenin
   yutulmasi (`SpeechDirector.cs:27-33`) ve konusmaci degisiminde flush'in oyuncunun
   sorusunu kesmesi (`SpeechDirector.cs:84-86`). Duzeltmeler semptom-basina (12 karlik
   prefix sezgisi, purge kaldirma); durum makinesi hala 4 statik alanla elle tutuluyor ve
   birim testi yok — kok uretec duruyor.
2. **(f, suphe — kod okumasiyla tespit, calistirilarak dogrulanmadi.** `FeedFinal`'daki
   purge kosulu (`SpeechDirector.cs:53`) konusmaci KONTROLU yapmadan "hic akmamis final =
   ikame" der. Akissiz her `FeedFinal` (oyuncu sorusu, kahin `7UL`) `_spokenChars ==
   remainder.Length` uretip `SpeechPlaybackHost.Flush()` tetikler (`SpeechDirector.cs:64`)
   — yani oyuncunun sesli sorusu kuyruktayken gelen akissiz kahin cevabi onu kesebilir;
   84-86'daki "yalnizca ayni-konusmaci ikamesi purge eder" niyetiyle celisir gorunuyor.
3. **(a) Tek-nokta tasarim.** `SpeechDirector` durumu, `SpeechPlaybackHost` (tek
   AudioSource) ve her iki backend tamami statik/singleton — ayni anda iki konusma ya da
   sahne-yerlestirilmis 3D ses imkansiz. `spatialBlend=0` yaninda "3D voices come with
   M3b.3" notu duruyor (`PiperSpeechSynth.cs:175`) ama M3b.3 oyuncu sesini getirdi, 3D'yi
   getirmedi — bayat yorum/odenmemis vaat.
4. **(e) COM/surec API kullanimi kacak kapilariyla dolu.** SAPI tamamen string-tabanli
   reflection (`WindowsSpeechService.cs:42-51,81-91`) — derleme-zamani guvence sifir;
   `_dead` bayragi HER istisnada kalici kapanir ve oturum boyunca geri acilmaz
   (`WindowsSpeechService.cs:56-57`). Ayni kalicilik Piper'da da var: tek surec olumu
   `_dead=true` yapar ve yeniden deneme yolu yoktur (`PiperSpeechSynth.cs:66,76`) — bir
   crash, oturumun kalanini SAPI'ye (o da olduyse sessizlige) mahkum eder.
5. **Elle JSON, iki yonde.** Giden: `JsonEscape` yalnizca `\` `"` `\n` `\r` kacislar
   (`PiperSpeechSynth.cs:138-139`) — diger kontrol karakterleri piper'in JSON satirini
   bozabilir. Gelen: `ReadNumSpeakers` substring taramasi (`PiperSpeechSynth.cs:122-136`);
   json bicimi degisirse sessizce 1 doner (tek-konusmaci moduna gizli dusus).
6. **Disk buyumesi.** Calinan wav'lar oturum icinde SILINMEZ — temizlik yalnizca surec
   dogumunda (`PiperSpeechSynth.cs:91-92`); uzun oturumda `tts-out` sinirsiz buyur
   (temporaryCachePath oldugu icin OS eninde sonunda toplar, ama oturum-ici tavan yok).
7. **Cumle bolucu naif.** `.` `!` `?` her gecende boler (`NpcVoiceSignatureService.cs:60`)
   — "Mr. Aldric" ya da "3.5" iki parca olur; `chunk.Length > 1` tek koruma (:63). SAPI
   yolunda 300 karakter kirpma uzun final kalanlarini sessizce keser
   (`WindowsSpeechService.cs:47`).
8. **Capraz-konusmaci final dedupe siralamasi.** `FeedFinal` `_lastFinal` karsilastirmasini
   `RetargetIfNeeded`'den ONCE yapar (`SpeechDirector.cs:42-44`) — B konusmacisinin finali
   A'nin son finaliyle birebir ayniysa hic seslendirilmez (kucuk ama gercek kenar durumu;
   testsiz).
9. **Olu/yetim API'ler:** `WindowsSpeechService.Speak` (:24-29) ve `StopSpeaking` (:61-73)
   icin repo'da cagiran bulunamadi; yorumdaki "proofs, notifications" kullanimlari ya
   kaldirilmis ya hic baglanmamis (dogrulanmadi).
10. **Genel kacak kapisi:** ses tamamen `#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN`
    arkasinda (`PiperSpeechSynth.cs:32,61,85`, `WindowsSpeechService.cs:33,63,77`) — diger
    platformlarda sistem derlenir ama sonsuza dek sessizdir; bu bilincli bir kapsam karari,
    gizli bir hata degil.
