---
name: Ember Vision Notes (Mami's words)
date: 2026-05-16
author: Mami (msbel5)
status: source of author intent
relationship_to_other_docs: |
  Mechanic docs in DOCS/mechanics/ remain canonical for implementation.
  This file is consulted for intent when atom-map decomposition has
  ambiguity. It does NOT supersede DOCS/EMBER_VISION_BIBLE.md or any
  mechanic doc; it complements them by recording Mami's voice directly.
---

# Ember Vision Notes — Mami's words

This document has three sections, in operational priority for Captain:

1. **Phase fences** — operating constraints Captain MUST respect when decomposing atoms. These are "not now" lines.
2. **Vision anchors** — 9-point checklist Captain references at every atom-map kickoff.
3. **Mami's verbatim text** — archive of the original message in Mami's own voice. Used for tone and emergent guidance, not for direct atom decomposition.

---

## 1. Phase fences (operating constraints — "not now")

Captain may NOT produce atom rows that implement the following before their owning faz:

| Fence | Owning faz | Tripwire if violated |
|---|---|---|
| **Memory state** (`ActorMemoryRecord`, memory pressure source, memory query) | Faz 9 | Atom rows that reference `memoryPressure`, `MemoryStore`, or `memory_decay` outside Faz 9 atom map fail audit per `inspector-audit-checklist.md` D. |
| **Shared NPC / party / DM tool surface** (isomorphic tool-call interfaces across multiple actor classes) | Faz 10 | Atom rows that introduce `INpcToolSurface`, `IPartyToolSurface`, or shared `ToolCallContext` types outside Faz 10 fail audit. Single-actor tool calls inside an existing faz are fine. |
| **LLM fallback wiring** (real or mock LLM client used as default execution path) | Faz 12 | Atom rows that import or call LLM clients outside Faz 12 fail audit. Tests may use deterministic mocks. |
| **Procedural genesis** (world-init questions, race/culture/civ/geography generation) | post-Faz 12 (dedicated faz to be opened) | Atom rows that generate world content procedurally fail audit until a dedicated faz is opened. |
| **Multiverse / 100K-year history / civs ascending to space** | scope aspiration only | These are not atom rows; they are anchors for design taste, not work. |
| **Dialog freedom UX** (Fallout-1 Ask-About / Hitchhiker free-text input) | Faz 9 | Atom rows that build free-text dialog parsing outside Faz 9 fail audit. The Sprint 1 narrative iskelet may be referenced and reused inside Faz 9 atoms, but not rewritten elsewhere. |

If Captain identifies a real need that crosses a fence, the response is:

1. Write a one-paragraph **fence breach proposal** in the sprint summary.
2. Stop. Do not write the atom row.
3. Mami decides whether to lift the fence. Captain does not lift fences.

---

## 2. Vision anchors (9-point reference)

At every atom-map kickoff doc, Captain quotes which anchors the sprint serves under a `## Vision anchors` heading per `inspector-audit-checklist.md` H. A sprint that serves zero anchors is internal scaffolding only and must be capped at one PR with explicit "internal-scaffolding" justification.

1. **Living-world over showroom.** Dünya oyuncudan bağımsız yaşar — NPC needs/mood/social bonds/ideologies tick whether the player is present or not. Lineage: Dwarf Fortress + RimWorld.
2. **Deterministic-first, LLM-last.** Simulation is authoritative. LLM is consulted only when the deterministic path genuinely cannot answer. The world stays the world; the LLM never writes it.
3. **Tool-calling layer is THE interpreter.** NPCs, party members, and the DM each hold deterministic tool surfaces. Tool calls read the simulation, do not mutate it directly. In stale state an NPC may escalate to the DM; the DM has its own tool surface.
4. **Procedural genesis at game-start.** A new game asks the player a small set of questions; from those, races, cultures, civilizations, geography, history, and multiverse structure are generated. Each new game is a new world.
5. **Data-driven extension.** New recipes, spells, jobs, events, etc. ship as data rows. C# changes only when a new operation kind is required. No enum branching for content.
6. **Morrowind-shaped presentation.** First-person and third-person, real-time with pause, character-perspective immersion. Not isometric. Not turn-based.
7. **Dialog freedom (Fallout 1 + Hitchhiker text-adventure).** The player phrases questions. NPC responses are generated from history + personality + culture + region + politics + economy + relationships + current psychology. Pre-written branching trees are the fallback, not the default.
8. **Systemic interaction (Divinity OS lineage).** Systems touch each other. Object manipulation, system-by-system reaction chains, weather x combat, faction x economy.
9. **DM as Storyteller (RimWorld Phoebe / Cassandra / Randy lineage).** AI DM module ships an explicit "Consult Fate" surface for the player and condition-checkpoint emergent storytelling. DM creates events from simulation state; it never writes story on top.

---

## 3. Mami's verbatim text (archive)

> The text below is Mami's original message, recorded verbatim for tone and intent. Captain reads it for taste, not for atom rows. Atom rows derive from Sections 1 and 2 above, never from Section 3 directly.

```
Ember RPG'nin temelinde bir hayal var: gerçekten yaşayan, deterministik, procedural bir dünya yaratmak.

Bu fikir benim eski CRPG sevgimden geliyor. Küçükken çok fazla FPS oynuyordum ama çoğunlukla yalnızdım. Daha sonra klasik CRPG'leri keşfetmeye başladım: Baldur's Gate 1-2, Fallout 1-2, Divinity Original Sin… Bu oyunlar bana başka hiçbir oyunun vermediği bir özgürlük hissi verdi.

Divinity Original Sin'de dünyadaki objeleri manipüle edebiliyorduk. Sistemler birbirine dokunuyordu. Oyuncu sadece combat yapan biri değildi; dünyayla fiziksel ve sistemsel olarak etkileşime giriyordu.

Ama özellikle Fallout 1 bende çok büyük bir iz bıraktı. NPC'lerle konuşurken "Ask about / Tell me about" tarzı diyalog seçenekleri vardı. Oyuncu hazır yazılmış birkaç seçenekle sınırlı hissetmiyordu. Dünyanın içindeki karakterlere gerçekten soru soruyor gibiydi. O hissi unutmadım.

Aynı dönemde The Hitchhiker's Guide to the Galaxy'nin radyo tiyatrosunu dinliyordum. Sonra BBC'nin yaptığı text adventure oyununu keşfettim. Oyuncu yazı yazıyordu ve dünya buna cevap veriyordu. Bu eski text-adventure ruhu ile klasik CRPG özgürlüğünün birleşimi bana inanılmaz büyülü geldi.

Ember RPG'nin ruhu tam olarak burada doğuyor:
Oyuncunun sadece "oyun oynadığı" değil, yaşayan bir dünyayla konuştuğu bir deneyim.

Bugün elimizde AI modülleri var. Ama hedef sadece AI kullanmak değil. Hedef; deterministik, procedural, sistemik bir dünyanın üzerine AI katmanı koymak.

Çünkü AI tek başına yeterli değil.
Arkasında yaşayan bir simulation olmak zorunda.

Bu yüzden Ember RPG'de dünya tamamen procedural üretilecek. Oyuncuya başlangıçta birkaç soru sorulacak ve bu cevaplara göre canlı türleri, kültürler, medeniyetler, tarih, coğrafya ve multiverse yapıları oluşacak.

Her şey data-driven olacak.
Her sistem genişletilebilir olacak.
Her yeni oyun gerçekten yeni bir dünya olacak.

Dünya sadece oyuncu için var olmayacak.
Oyuncu hiçbir şey yapmasa bile dünya yaşamaya devam edecek.

NPC'ler kendi ihtiyaçlarına sahip olacak:
Mutluluk, korku, sıcaklık, açlık, sosyal bağlar, ideolojiler, hedefler…

Koloniler oluşacak.
Savaşlar çıkacak.
Ekonomiler gelişecek.
Irklar evrimleşecek.
Teknolojiler ilerleyecek.

Belki dünya yüz bin yıl yaşayacak.
Belki medeniyetler uzaya çıkacak.
Belki oyuncu bir multiverse içinde yüzlerce gezegen dolaşacak.
Belki bazı gezegenlerde yüz binlerce yaşayan NPC olacak.

Buradaki en büyük ilham kaynaklarından biri Dwarf Fortress ve RimWorld.
Çünkü onlar dünyayı sadece dekor olarak değil, çalışan bir simulation olarak görüyor.

Ama Ember RPG bunu colony simulator gibi sunmayacak.
Oyuncu dünyayı bir karakterin gözünden yaşayacak.

Yani presentation tarafında hedefimiz:
Morrowind tarzı immersion ve FRP hissi.

Oyuncu bir hana girecek.
Bir NPC'ye yaklaşacak.
İstediği şeyi sorabilecek.
Kendi cümlelerini kurabilecek.

Ve sistem sadece hazır cevap dönmeyecek.

NPC'nin geçmişi,
kişiliği,
kültürü,
bulunduğu bölge,
politik durumu,
ekonomik şartları,
ilişkileri
ve mevcut psikolojisi üzerinden mantıklı cevaplar üretilecek.

Üstelik oyuncu onlarla konuşmadığında da yaşamaya devam edecekler.

Bu dünyanın üstünde bir AI layer olacak.
AI burada "dünyanın yerine geçen şey" değil;
dünyanın yorumlayıcısı olacak.

AI modülleri tool-calling kullanacak.
Simulation'dan veri okuyacak.
Kuralları bozmayacak.
Deterministik sistemin üstünde çalışacak.

Bunun üstünde ayrıca bir DM (Dungeon Master) sistemi olacak.

DM sistemi klasik tabletop FRP ruhunu yaşatacak.
Oyuncu DM'ye danışabilecek.
Sorular sorabilecek.
"Consult Fate" gibi sistemlerle kader yorumları alabilecek.

Ve belirli condition checkpoint'leri gerçekleştiğinde DM sistemi hikâyeyi yönlendirecek.
Yeni olaylar yaratacak.
Dünyadaki sistemleri kullanarak emergent storytelling oluşturacak.

CRITICAL ADDITION (recorded later by Mami):
DM ve NPC'ler ve party member'lar tool call yapabilecek. Bazen NPC'ler de DM'e başvuracak. Deterministic tool call'lar olacak. Stale state'lerde DM de kendi tool call'ını kullanabilecek. Yani üç aktör sınıfı (NPC, party member, DM) deterministic tool-call holding actors.

Yani Ember RPG'nin amacı sadece bir oyun yapmak değil.

Hedef:

Fallout 1'in konuşma özgürlüğünü,
Hitchhiker's Guide'ın text-adventure ruhunu,
Divinity Original Sin'in sistemsel özgürlüğünü,
Dwarf Fortress ve RimWorld'ün yaşayan procedural dünyasını,
ve Morrowind'in immersive FRP hissini

tek bir deterministik simulation içinde birleştirmek.

Ember RPG bir "AI RPG" değil.

Ember RPG, yaşayan bir dünyanın üzerine kurulmuş,
gerçek roleplay özgürlüğünü hedefleyen,
deterministik bir simulation universe.
```
