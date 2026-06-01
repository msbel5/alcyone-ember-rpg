// Why this file is intentionally long: it implements the full PRD loading-screen lifecycle, visible progress, tips, backdrop, and fade behavior.
using System;
using System.Collections.Generic;
using System.IO;
using EmberCrpg.Simulation.Generation;
using EmberCrpg.Ui.Foundation;
using UnityEngine;

namespace EmberCrpg.Presentation.Ember.Loading
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        private const float TipRotationSeconds = 4f;
        private const float EllipsisCycleSeconds = 0.5f;
        private const float FadeInSeconds = 0.2f;
        private const float FadeOutSeconds = 0.3f;

        private static readonly string[] LoadingTips =
        {
            "Press Space to pause time in the middle of combat.",
            "Right-click a spell icon to inspect its details.",
            "Rest replenishes spells and health, but travel at night is risky.",
            "Some enemies ignore weak enchantment tiers.",
            "Check your journal for quest updates.",
            "Save often and keep multiple slots."
        };

        private IUiPanel _panel;
        private LoadingScreenContext _context = new LoadingScreenContext(string.Empty, string.Empty, "load");
        private float _progress;
        private int _currentTipIndex;
        private int _ellipsisFrame;
        private float _tipTimer;
        private float _ellipsisTimer;
        private bool _tipRotationRunning;
        private bool _inputBlocked;
        private bool _visible;
        private bool _fadingIn;
        private bool _fadingOut;
        private float _fadeTimer;
        private string _title = "Loading";
        private readonly HashSet<string> _missingBackdropLogged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public event Action<Dictionary<string, string>> Shown;
        public event Action Dismissed;

        private void Update()
        {
            if (!_visible) return;

            var delta = Time.unscaledDeltaTime;
            if (_tipRotationRunning)
            {
                _tipTimer += delta;
                if (_tipTimer >= TipRotationSeconds)
                {
                    _tipTimer = 0f;
                    AdvanceTip();
                }
            }

            TickEllipsisAnimation(delta);
            TickFade(delta);
        }

        public void Show(string title, string subtitle)
        {
            SetTitle(title);
            SetAreaName(subtitle);
            ShowForContext(new LoadingScreenContext(string.Empty, subtitle, "load"));
        }

        public void ShowForContext(LoadingScreenContext context)
        {
            if (_panel == null) _panel = UiSurfaceLocator.Current?.Mount("LoadingScreen");
            _context = context ?? new LoadingScreenContext(string.Empty, string.Empty, "load");
            _visible = true;
            _fadingOut = false;
            SetInputBlocking(true);
            SetLoadingType(_context.LoadingType);
            SetAreaName(_context.AreaName);
            var texture = LoadBackdropForArea(_context.AreaId);
            ApplyBackdrop(texture);
            // Show the ember logo on every loading screen (user request). Prefer the full logo,
            // fall back to the compact mark; both are forge-generated under Assets/Generated/Core.
            var logo = TryLoadGeneratedTexture("logo_full") ?? TryLoadGeneratedTexture("logo_compact");
            if (logo != null) _panel?.SetThumbnail("thumbnail", logo);
            _panel?.SetVisible("root", true);
            StartTipRotation();
            FadeIn();
            Shown?.Invoke(_context.ToDictionary());
        }

        public void SetProgress(float normalized, string currentLabel)
        {
            _progress = Mathf.Clamp01(normalized);
            _panel?.SetProgress("progress", _progress);
            _panel?.SetText("current", currentLabel ?? string.Empty);
        }

        public void LogLine(UiLogSeverity severity, string line)
        {
            _panel?.LogLine("log", severity, line ?? string.Empty);
        }

        public void ShowThumbnail(Texture2D texture, string caption)
        {
            _panel?.SetThumbnail("thumbnail", texture);
            _panel?.SetText("caption", caption ?? string.Empty);
        }

        public void Dismiss()
        {
            if (_panel == null) return;
            if (!_visible)
            {
                HideInternal();
                return;
            }

            StopTipRotation();
            if (Application.isBatchMode)
            {
                HideInternal();
                return;
            }

            FadeOut();
        }

        public bool IsVisibleLoading()
        {
            return _visible;
        }

        public void SetLoadingContext(LoadingScreenContext context)
        {
            _context = context ?? new LoadingScreenContext(string.Empty, string.Empty, "load");
            SetAreaId(_context.AreaId);
            SetAreaName(_context.AreaName);
            SetLoadingType(_context.LoadingType);
        }

        public LoadingScreenContext GetLoadingContext()
        {
            return _context;
        }

        public void SetAreaId(string areaId)
        {
            _context = _context.WithAreaId(areaId ?? string.Empty);
            ApplyBackdrop(LoadBackdropForArea(_context.AreaId));
        }

        // Why two writes used to land here: the panel reserved both an "area" header and a
        // "subtitle" line, but rendering them both with the same string surfaced "Ember Boot"
        // twice on screen. Keep the area header (typographic emphasis) and leave subtitle empty
        // so the panel layout stays consistent without the visual duplicate.
        public void SetAreaName(string areaName)
        {
            _context = _context.WithAreaName(areaName ?? string.Empty);
            _panel?.SetText("area", _context.AreaName);
            _panel?.SetText("subtitle", string.Empty);
        }

        public void SetLoadingType(string loadingType)
        {
            _context = _context.WithLoadingType(loadingType);
            _panel?.SetText("title", _title);
            // The "loading" label already shows "Entering area…" (with ellipsis); writing the same
            // prefix into "status" produced a visual duplicate. Keep status empty so the panel's
            // status row stays a hairline gap and the loading row owns the active state.
            _panel?.SetText("status", string.Empty);
            _panel?.SetText("loading", BuildLoadingLabelText());
        }

        public Texture2D LoadBackdropForArea(string areaId)
        {
            if (string.IsNullOrWhiteSpace(areaId))
                return TryLoadGeneratedTexture("splash_background") ?? Resources.Load<Texture2D>("Loading/generic");

            var normalized = areaId.Trim();
            var specific = Resources.Load<Texture2D>("Loading/" + normalized);
            if (specific != null) return specific;

            // Runtime fallback: pick up forge-generated PNGs from disk under <root>/Assets/Generated/Core/.
            // Map known area ids to manifest entry ids so loading screens get a real backdrop instead of a blank.
            var generated = TryLoadGeneratedTexture(MapAreaIdToGeneratedEntry(normalized));
            if (generated != null) return generated;

            // Last fallback: the universal splash_background if it exists.
            var splash = TryLoadGeneratedTexture("splash_background");
            if (splash != null) return splash;

            if (_missingBackdropLogged.Add(normalized))
                Debug.LogWarning("[LoadingScreen] Missing backdrop for area '" + normalized + "', using generic fallback.");

            return Resources.Load<Texture2D>("Loading/generic");
        }

        private static string MapAreaIdToGeneratedEntry(string areaId)
        {
            // Loading contexts ("boot", "main_menu", "character_creation", "worldgen") all share splash_background
            // until per-area generated backdrops exist; scene-name areas get their own entry if generated.
            switch (areaId.ToLowerInvariant())
            {
                case "boot":
                case "main_menu":
                case "mainmenu":
                case "character_creation":
                case "worldgen":
                    return "splash_background";
                default:
                    return areaId;
            }
        }

        private static Texture2D TryLoadGeneratedTexture(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId)) return null;
            var parent = Directory.GetParent(Application.dataPath);
            var root = parent != null ? parent.FullName : Application.dataPath;
            var path = Path.Combine(root, "Assets", "Generated", "Core", entryId + ".png");
            if (!File.Exists(path)) return null;
            if (!GeneratedAssetProvenance.IsFreshCoreAsset(entryId, path)) return null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    return tex;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LoadingScreen] Failed to load generated texture '" + entryId + "': " + ex.Message);
                return null;
            }
        }

        public void ApplyBackdrop(Texture2D texture)
        {
            _panel?.SetThumbnail("backdrop", texture);
        }

        public void StartTipRotation()
        {
            _tipRotationRunning = true;
            _tipTimer = 0f;
            _panel?.SetText("tip", GetCurrentTip());
        }

        public void StopTipRotation()
        {
            _tipRotationRunning = false;
        }

        public void AdvanceTip()
        {
            _currentTipIndex = (_currentTipIndex + 1) % LoadingTips.Length;
            _panel?.SetText("tip", GetCurrentTip());
        }

        public string GetCurrentTip()
        {
            return LoadingTips[_currentTipIndex];
        }

        public float GetProgress()
        {
            return _progress;
        }

        public void TickEllipsisAnimation(float deltaTime)
        {
            if (!_visible) return;

            _ellipsisTimer += Mathf.Max(0f, deltaTime);
            if (_ellipsisTimer >= EllipsisCycleSeconds)
            {
                _ellipsisTimer = 0f;
                _ellipsisFrame = (_ellipsisFrame + 1) % 4;
                _panel?.SetText("loading", BuildLoadingLabelText());
            }
        }

        public string BuildLoadingLabelText()
        {
            return StatusPrefixFor(_context.LoadingType) + new string('.', _ellipsisFrame);
        }

        public void FadeIn()
        {
            _fadingOut = false;
            _fadingIn = true;
            _fadeTimer = 0f;
            _panel?.SetProgress("fade", 0f);
        }

        public void FadeOut()
        {
            _fadingIn = false;
            _fadingOut = true;
            _fadeTimer = 0f;
        }

        public void SetInputBlocking(bool blocked)
        {
            _inputBlocked = blocked;
            _panel?.SetVisible("inputBlock", blocked);
        }

        public void SetTitle(string title)
        {
            _title = string.IsNullOrWhiteSpace(title) ? "Loading" : title;
            _panel?.SetText("title", _title);
        }

        public void Hide()
        {
            Dismiss();
        }

        private void TickFade(float delta)
        {
            if (_fadingIn)
            {
                _fadeTimer += delta;
                var alpha = FadeInSeconds <= 0f ? 1f : Mathf.Clamp01(_fadeTimer / FadeInSeconds);
                _panel?.SetProgress("fade", alpha);
                if (_fadeTimer >= FadeInSeconds)
                {
                    _fadingIn = false;
                    _panel?.SetProgress("fade", 1f);
                }
            }

            if (_fadingOut)
            {
                _fadeTimer += delta;
                var alpha = FadeOutSeconds <= 0f ? 0f : 1f - Mathf.Clamp01(_fadeTimer / FadeOutSeconds);
                _panel?.SetProgress("fade", alpha);
                if (_fadeTimer >= FadeOutSeconds)
                {
                    _fadingOut = false;
                    HideInternal();
                }
            }
        }

        private void HideInternal()
        {
            _visible = false;
            SetInputBlocking(false);
            if (_panel != null) UiSurfaceLocator.Current?.Unmount(_panel);
            _panel = null;
            Dismissed?.Invoke();
        }

        private static string StatusPrefixFor(string loadingType)
        {
            if (string.Equals(loadingType, "save", StringComparison.OrdinalIgnoreCase)) return "Saving";
            if (string.Equals(loadingType, "area_transition", StringComparison.OrdinalIgnoreCase)) return "Entering area";
            return "Loading";
        }
    }
}
