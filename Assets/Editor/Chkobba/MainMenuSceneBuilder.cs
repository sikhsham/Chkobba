#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Chkobba.EditorTools
{
    /// <summary>
    /// Builds the entire MainMenu scene programmatically (Chkobba > Build Main Menu Scene).
    /// Deterministic: can be re-run to regenerate the scene from code.
    /// Layout mirrors the approved V1 prototype (390x844 portrait reference).
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        private const string AssetsRoot = "Assets/UI/MainMenu";
        private const string IconsRoot = AssetsRoot + "/icons";
        private const string FontsRoot = "Assets/UI/Fonts";

        [MenuItem("Chkobba/Build Main Menu Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- camera ---------------------------------------------------
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ChkobbaPalette.Hex(0x17130F);
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();

            // ---- canvas + event system ------------------------------------
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Project is Input-System-only; the legacy StandaloneInputModule errors at runtime.
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var orient = new GameObject("Orientation").AddComponent<Chkobba.ScreenOrientationController>();
            orient.orientation = ScreenOrientation.Portrait;

            // All menu UI lives under a full-stretch RectTransform parented to the
            // overlay canvas — elements outside the canvas hierarchy never render.
            var root = new GameObject("MainMenu", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            Stretch((RectTransform)root.transform);

            // ---- backdrop stack ---------------------------------------------
            // Painting is 720x1080 (2:3); canvas is 390x844. Width overscan keeps it gapless.
            var paintGo = new GameObject("CafeScene", typeof(RectTransform));
            paintGo.transform.SetParent(root.transform, false);
            var paintImg = paintGo.AddComponent<Image>();
            paintImg.sprite = Load<Sprite>(AssetsRoot + "/cafe-scene.jpg");
            paintImg.preserveAspect = true;
            paintImg.raycastTarget = false;
            Stretch((RectTransform)paintGo.transform);
            ((RectTransform)paintGo.transform).sizeDelta = new Vector2(240f, 0f);

            var melt = NewImage(root, "Melt", AssetsRoot + "/grad-melt.png", Color.white);
            FullStretchTop((RectTransform)melt.transform, 335f, 210f);

            var table = NewImage(root, "Table", AssetsRoot + "/grad-table.png", Color.white);
            FullStretchBottom((RectTransform)table.transform, 500f);

            var scrim = NewImage(root, "Scrim", AssetsRoot + "/grad-scrim.png", Color.white);
            FullStretchTop((RectTransform)scrim.transform, 0f, 250f);

            // ---- logo ---------------------------------------------------------
            var logo = new GameObject("Logo", typeof(RectTransform));
            logo.transform.SetParent(root.transform, false);
            var lrt = (RectTransform)logo.transform;
            lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0f, -52f);
            lrt.sizeDelta = new Vector2(0f, 190f);

            var wordmark = NewText(logo, "ArWordmark", "شكبة", FontsRoot + "/Rakkas-Regular SDF.asset", 80f, ChkobbaPalette.CobaltDeep);
            var wrt = (RectTransform)wordmark.transform;
            wrt.anchorMin = new Vector2(0f, 1f); wrt.anchorMax = new Vector2(1f, 1f);
            wrt.pivot = new Vector2(0.5f, 1f);
            wrt.anchoredPosition = Vector2.zero;
            wrt.sizeDelta = new Vector2(0f, 92f);

            var latin = NewText(logo, "Latin", "CHKOBBA", FontsRoot + "/Amiri-Bold SDF.asset", 14f, ChkobbaPalette.Cobalt, 18f);
            var lrt2 = (RectTransform)latin.transform;
            lrt2.anchorMin = new Vector2(0f, 1f); lrt2.anchorMax = new Vector2(1f, 1f);
            lrt2.pivot = new Vector2(0.5f, 1f);
            lrt2.anchoredPosition = new Vector2(0f, -96f);
            lrt2.sizeDelta = new Vector2(0f, 20f);

            var tagline = NewText(logo, "Tagline", "لعبة الكارطة التونسية", FontsRoot + "/Amiri-Regular SDF.asset", 14f,
                ChkobbaPalette.WithAlpha(ChkobbaPalette.Ink, 0.78f));
            var trt2 = (RectTransform)tagline.transform;
            trt2.anchorMin = new Vector2(0f, 1f); trt2.anchorMax = new Vector2(1f, 1f);
            trt2.pivot = new Vector2(0.5f, 1f);
            trt2.anchoredPosition = new Vector2(0f, -122f);
            trt2.sizeDelta = new Vector2(0f, 24f);

            // ---- chips ---------------------------------------------------------
            var soundChipGo = NewIconButton(root, "SoundChip", IconsRoot + "/icon-sound.png", AssetsRoot + "/chip.png",
                new Vector2(44, 44), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -70f), ChkobbaPalette.CobaltDeep);
            var settingsChipGo = NewIconButton(root, "SettingsChip", IconsRoot + "/icon-settings.png", AssetsRoot + "/chip.png",
                new Vector2(44, 44), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -70f), ChkobbaPalette.CobaltDeep);

            // ---- fan -----------------------------------------------------------
            var fan = new GameObject("Fan", typeof(RectTransform));
            fan.transform.SetParent(root.transform, false);
            var frt = (RectTransform)fan.transform;
            frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(1f, 0f);
            frt.pivot = new Vector2(0.5f, 0f);
            frt.anchoredPosition = new Vector2(0f, 58f);
            frt.sizeDelta = new Vector2(0f, 300f);

            // L->R as in the prototype (x offsets mirror the CSS arc)
            var defs = new List<CardDef>
            {
                new CardDef("Ranking", "الترتيب", "CLASSEMENT", new Vector2(80,118), -143f, 9f, -8f, 1, true,  false, ChkobbaPalette.Olive),
                new CardDef("Rules",   "قواعد",   "LES RÈGLES",  new Vector2(80,118), -75f,  4f, -4f, 2, false, false, ChkobbaPalette.Brass),
                new CardDef("Play",    "إلعب",    "JOUER",       new Vector2(94,138),   0f,  9f,  0f, 6, false, true,  ChkobbaPalette.Red),
                new CardDef("Friends", "مع صحابك","ENTRE AMIS",  new Vector2(80,118),  75f,  4f,  4f, 2, false, false, ChkobbaPalette.Cobalt),
                new CardDef("Profile", "حسابي",   "PROFIL",      new Vector2(80,118), 143f,  9f,  8f, 1, false, false, ChkobbaPalette.Red),
            };

            RectTransform rankingCard = null;
            var cardRects = new List<RectTransform>();
            foreach (var d in defs)
            {
                var card = BuildCard(frt, d);
                cardRects.Add(card);
                if (d.soon) rankingCard = card;
            }

            // ---- toast ----------------------------------------------------------
            var toastGo = new GameObject("Toast", typeof(RectTransform));
            toastGo.transform.SetParent(root.transform, false);
            var toastRt = (RectTransform)toastGo.transform;
            toastRt.anchorMin = new Vector2(0f, 0f); toastRt.anchorMax = new Vector2(1f, 0f);
            toastRt.pivot = new Vector2(0.5f, 0f);
            toastRt.anchoredPosition = new Vector2(0f, 225f);
            toastRt.sizeDelta = new Vector2(0f, 46f);
            var toastGroup = toastGo.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            var toastBg = toastGo.AddComponent<Image>();
            toastBg.sprite = Load<Sprite>(AssetsRoot + "/sheet-bg.png");
            toastBg.type = Image.Type.Sliced;
            toastBg.color = ChkobbaPalette.Paper2;
            var toastLabel = NewText(toastGo, "Label", "", FontsRoot + "/Amiri-Regular SDF.asset", 13f, ChkobbaPalette.Ink);
            Stretch((RectTransform)toastLabel.transform, 12f, 5f);

            // ---- backdrop + sheets -----------------------------------------------
            var backdropGo = new GameObject("Backdrop", typeof(RectTransform));
            backdropGo.transform.SetParent(root.transform, false);
            Stretch((RectTransform)backdropGo.transform);
            var backdropImg = backdropGo.AddComponent<Image>();
            backdropImg.color = ChkobbaPalette.WithAlpha(ChkobbaPalette.Hex(0x121E32), 0.42f);
            var backdropGroup = backdropGo.AddComponent<CanvasGroup>();
            backdropGroup.alpha = 0f;
            backdropGroup.blocksRaycasts = false;
            var backdropBtn = backdropGo.AddComponent<Button>();
            backdropBtn.transition = Selectable.Transition.None;

            var rulesSheet = BuildSheet(root.transform, "RulesSheet", "قواعد اللعبة", "LES RÈGLES · CHKOBBA");
            var settingsSheet = BuildSheet(root.transform, "SettingsSheet", "الإعدادات", "RÉGLAGES");

            // ---- controller --------------------------------------------------------
            var menu = root.AddComponent<Chkobba.MainMenuController>();
            menu.chipIcon = soundChipGo.transform.Find("Icon").GetComponent<Image>();
            menu.soundOnIcon = Load<Sprite>(IconsRoot + "/icon-sound.png");
            menu.mutedIcon = Load<Sprite>(IconsRoot + "/icon-muted.png");
            menu.toggleOn = Load<Sprite>(AssetsRoot + "/toggle-on.png");
            menu.toggleOff = Load<Sprite>(AssetsRoot + "/toggle-off.png");
            menu.segOnSprite = Load<Sprite>(AssetsRoot + "/seg-on.png");
            menu.segOffSprite = Load<Sprite>(AssetsRoot + "/seg-off.png");
            menu.soundToggle = settingsSheet.transform.Find("Body/SoundRow/Toggle").GetComponent<Image>();
            menu.musicToggle = settingsSheet.transform.Find("Body/MusicRow/Toggle").GetComponent<Image>();
            menu.langArabic = settingsSheet.transform.Find("Body/LangRow/Seg/Arabic").GetComponent<Image>();
            menu.langFrench = settingsSheet.transform.Find("Body/LangRow/Seg/French").GetComponent<Image>();
            menu.rulesSheet = rulesSheet;
            menu.settingsSheet = settingsSheet;
            menu.backdrop = backdropGroup;

            var toast = toastGo.AddComponent<Chkobba.Toast>();
            SetField(toast, "group", toastGroup);
            SetField(toast, "label", toastLabel);
            menu.toast = toast;

            var ranking = rankingCard.gameObject.AddComponent<Chkobba.RankingCard>();
            SetField(ranking, "menu", menu);
            menu.rankingCard = ranking;

            var playAnim = root.AddComponent<Chkobba.PlayCardsAnimator>();
            SetField(playAnim, "cards", cardRects.ToArray());
            menu.playAnimator = playAnim;

            // Arabic shaping: one preprocessor per TMP text (font-aware fallback)
            foreach (var txt in root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
            {
                var shaperGo = new GameObject("Shaper", typeof(Chkobba.ArabicTextShaper));
                shaperGo.transform.SetParent(txt.transform, false);
                shaperGo.GetComponent<Chkobba.ArabicTextShaper>().Configure(txt);
            }

            // ---- save ------------------------------------------------------------------
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
            Debug.Log("[Chkobba] MainMenu scene built: Assets/Scenes/MainMenu.unity");
        }

        // ---- cards -----------------------------------------------------------

        private class CardDef
        {
            public string id, label, caption;
            public Vector2 size;
            public float x, y, rot;
            public int z;
            public bool soon, play;
            public Color color;

            public CardDef(string id, string label, string caption, Vector2 size,
                float x, float y, float rot, int z, bool soon, bool play, Color color)
            {
                this.id = id; this.label = label; this.caption = caption; this.size = size;
                this.x = x; this.y = y; this.rot = rot; this.z = z; this.soon = soon; this.play = play; this.color = color;
            }
        }

        private static RectTransform BuildCard(RectTransform fan, CardDef d)
        {
            var go = new GameObject(d.id, typeof(RectTransform));
            go.transform.SetParent(fan, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = d.size;
            rt.anchoredPosition = new Vector2(d.x, d.y);
            rt.localRotation = Quaternion.Euler(0f, 0f, d.rot);

            var img = go.AddComponent<Image>();
            img.sprite = Load<Sprite>(AssetsRoot + (d.play ? "/card-bg-play.png" : "/card-bg.png"));
            img.type = Image.Type.Sliced;
            img.color = d.soon ? ChkobbaPalette.WithAlpha(Color.white, 0.82f) : Color.white;

            var icon = NewImage(go, "Icon", IconsRoot + "/icon-settings.png",
                d.play ? ChkobbaPalette.Red : ChkobbaPalette.WithAlpha(ChkobbaPalette.Ink, 0.85f));
            var irt = (RectTransform)icon.transform;
            irt.sizeDelta = new Vector2(30f, 30f);
            irt.anchoredPosition = new Vector2(0f, d.play ? 26f : 20f);

            var label = NewText(go, "Label", d.label, FontsRoot + "/Amiri-Bold SDF.asset", d.play ? 24f : 14f,
                d.play ? ChkobbaPalette.Red : ChkobbaPalette.Ink);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(1f, 1f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0f, d.play ? -62f : -52f);
            lrt.sizeDelta = new Vector2(-12f, 28f);

            var caption = NewText(go, "Caption", d.caption, FontsRoot + "/Amiri-Regular SDF.asset", 8f,
                ChkobbaPalette.WithAlpha(d.play ? ChkobbaPalette.Red : ChkobbaPalette.Ink, 0.55f));
            var crt = (RectTransform)caption.transform;
            crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0f, 9f);
            crt.sizeDelta = new Vector2(-10f, 12f);

            if (d.soon)
            {
                var pill = NewImage(go, "SoonPill", AssetsRoot + "/pill.png", Color.white);
                var prt = (RectTransform)pill.transform;
                prt.anchorMin = new Vector2(0.5f, 1f); prt.anchorMax = new Vector2(0.5f, 1f);
                prt.anchoredPosition = new Vector2(0f, 11f);
                prt.sizeDelta = new Vector2(56f, 21f);
                var pillText = NewText(pill.gameObject, "Label", "قريب", FontsRoot + "/Amiri-Bold SDF.asset", 11f, ChkobbaPalette.Hex(0xFFF6EA));
                Stretch((RectTransform)pillText.transform);
            }

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            return rt;
        }

        // ---- sheets -----------------------------------------------------------

        private static Chkobba.SheetPanel BuildSheet(Transform root, string name, string title, string subtitle)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.SetActive(false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 440f);

            var bg = go.AddComponent<Image>();
            bg.sprite = Load<Sprite>(AssetsRoot + "/sheet-bg.png");
            bg.type = Image.Type.Sliced;
            bg.color = ChkobbaPalette.Paper;

            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(go.transform, false);
            var hrt = (RectTransform)header.transform;
            hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -12f);
            hrt.sizeDelta = new Vector2(-40f, 48f);

            var ttl = NewText(header, "Title", title, FontsRoot + "/Amiri-Bold SDF.asset", 19f, ChkobbaPalette.CobaltDeep);
            var ttlRt = (RectTransform)ttl.transform;
            ttlRt.anchorMin = new Vector2(0f, 0f); ttlRt.anchorMax = new Vector2(1f, 1f);
            ttlRt.offsetMin = Vector2.zero;
            ttlRt.offsetMax = new Vector2(-52f, 0f);
            ttl.alignment = TMPro.TextAlignmentOptions.Right;

            var sub = NewText(header, "Subtitle", subtitle, FontsRoot + "/Amiri-Regular SDF.asset", 10f,
                ChkobbaPalette.WithAlpha(ChkobbaPalette.Ink, 0.45f));
            var subRt = (RectTransform)sub.transform;
            subRt.anchorMin = new Vector2(0f, 0f); subRt.anchorMax = new Vector2(1f, 0f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -30f);
            subRt.sizeDelta = new Vector2(-52f, 16f);
            sub.alignment = TMPro.TextAlignmentOptions.Right;

            var close = NewIconButton(header, "Close", IconsRoot + "/icon-x.png", AssetsRoot + "/chip.png",
                new Vector2(40, 40), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, ChkobbaPalette.CobaltDeep);

            var body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(go.transform, false);
            var brt = (RectTransform)body.transform;
            brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(20f, 16f);
            brt.offsetMax = new Vector2(-20f, -70f);

            if (name == "RulesSheet") BuildRulesBody(brt);
            else BuildSettingsBody(brt);

            var panel = go.AddComponent<Chkobba.SheetPanel>();
            SetField(panel, "panel", rt);
            return panel;
        }

        private static void BuildRulesBody(RectTransform body)
        {
            string[] rules =
            {
                "٤٠ كارطة وتلعبها زوز لاعبين ولا فريقين — الهدف: تجمع أكثر كارطة وتسبق بزوز نقاط.",
                "طيّح كارطة تسوى كارطة من اللي على الطاولة، ولا مجموع كارطتين — تسوى؟ الكل عندك.",
                "كنست الطاولة كاملة؟ هاذيك «شكبة»! نقطة زايدة، وتصيّح «شكبة!» بأعلى صوتك.",
                "النقاط: أكثر كارطة · الدناري · السبع · البرميلة — وكل شكبة نقطة."
            };
            Color[] numColors = { ChkobbaPalette.Cobalt, ChkobbaPalette.Brass, ChkobbaPalette.Red, ChkobbaPalette.Olive };
            string[] numerals = { "١", "٢", "٣", "٤" };

            for (int i = 0; i < rules.Length; i++)
            {
                var row = new GameObject("Rule" + (i + 1), typeof(RectTransform));
                row.transform.SetParent(body, false);
                var rr = (RectTransform)row.transform;
                rr.anchorMin = new Vector2(0f, 1f); rr.anchorMax = new Vector2(1f, 1f);
                rr.pivot = new Vector2(0.5f, 1f);
                rr.anchoredPosition = new Vector2(0f, -i * 80f);
                rr.sizeDelta = new Vector2(0f, 74f);

                // RTL: number circle on the right, text flows left of it
                var circle = NewImage(row, "Num", AssetsRoot + "/circle.png", numColors[i]);
                var nrt = (RectTransform)circle.transform;
                nrt.anchorMin = new Vector2(1f, 1f); nrt.anchorMax = new Vector2(1f, 1f);
                nrt.pivot = new Vector2(0.5f, 0.5f);
                nrt.anchoredPosition = new Vector2(-14f, -16f);
                nrt.sizeDelta = new Vector2(28f, 28f);
                var numText = NewText(circle.gameObject, "Label", numerals[i], FontsRoot + "/Amiri-Bold SDF.asset", 14f, Color.white);
                Stretch((RectTransform)numText.transform);

                var text = NewText(row, "Text", rules[i], FontsRoot + "/Amiri-Regular SDF.asset", 13f, ChkobbaPalette.Ink);
                var trt = (RectTransform)text.transform;
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = new Vector2(-44f, 0f);
                text.alignment = TMPro.TextAlignmentOptions.TopRight;
            }
        }

        private static void BuildSettingsBody(RectTransform body)
        {
            BuildSettingRow(body, "SoundRow", "الصوت", "أصوات الكارطة والقهوة", 0f);
            BuildSettingRow(body, "MusicRow", "الموسيقى", "عود ومالوف خفيف", 62f);
            BuildLangRow(body);
        }

        private static void BuildSettingRow(RectTransform body, string name, string title, string sub, float y)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(body, false);
            var rr = (RectTransform)row.transform;
            rr.anchorMin = new Vector2(0f, 1f); rr.anchorMax = new Vector2(1f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.anchoredPosition = new Vector2(0f, -y);
            rr.sizeDelta = new Vector2(0f, 56f);

            var titleT = NewText(row, "Title", title, FontsRoot + "/Amiri-Bold SDF.asset", 15f, ChkobbaPalette.Ink);
            var trt = (RectTransform)titleT.transform;
            trt.anchorMin = new Vector2(1f, 0.5f); trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(1f, 0.5f);
            trt.anchoredPosition = new Vector2(-64f, 8f);
            trt.sizeDelta = new Vector2(240f, 22f);
            titleT.alignment = TMPro.TextAlignmentOptions.Right;

            var subT = NewText(row, "Sub", sub, FontsRoot + "/Amiri-Regular SDF.asset", 10f,
                ChkobbaPalette.WithAlpha(ChkobbaPalette.Ink, 0.45f));
            var srt = (RectTransform)subT.transform;
            srt.anchorMin = new Vector2(1f, 0.5f); srt.anchorMax = new Vector2(1f, 0.5f);
            srt.pivot = new Vector2(1f, 0.5f);
            srt.anchoredPosition = new Vector2(-64f, -9f);
            srt.sizeDelta = new Vector2(240f, 16f);
            subT.alignment = TMPro.TextAlignmentOptions.Right;

            // toggle (left side in RTL layout)
            NewIconButton(row, "Toggle", null, AssetsRoot + "/toggle-on.png",
                new Vector2(52, 32), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, Color.white);
        }

        private static void BuildLangRow(RectTransform body)
        {
            var row = new GameObject("LangRow", typeof(RectTransform));
            row.transform.SetParent(body, false);
            var rr = (RectTransform)row.transform;
            rr.anchorMin = new Vector2(0f, 1f); rr.anchorMax = new Vector2(1f, 1f);
            rr.pivot = new Vector2(0.5f, 1f);
            rr.anchoredPosition = new Vector2(0f, -124f);
            rr.sizeDelta = new Vector2(0f, 48f);

            var titleT = NewText(row, "Title", "اللغة", FontsRoot + "/Amiri-Bold SDF.asset", 15f, ChkobbaPalette.Ink);
            var trt = (RectTransform)titleT.transform;
            trt.anchorMin = new Vector2(1f, 0.5f); trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(1f, 0.5f);
            trt.anchoredPosition = new Vector2(-64f, 0f);
            trt.sizeDelta = new Vector2(240f, 22f);
            titleT.alignment = TMPro.TextAlignmentOptions.Right;

            var seg = new GameObject("Seg", typeof(RectTransform));
            seg.transform.SetParent(row.transform, false);
            var srt = (RectTransform)seg.transform;
            srt.anchorMin = new Vector2(0f, 0.5f); srt.anchorMax = new Vector2(0f, 0.5f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(164f, 40f);

            NewIconButton(seg, "Arabic", null, AssetsRoot + "/seg-on.png", new Vector2(80, 40),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(41f, 0f), Color.white);
            var arLabel = new GameObject("Label", typeof(RectTransform));
            arLabel.transform.SetParent(seg.transform.Find("Arabic"), false);
            var tmpAr = arLabel.AddComponent<TMPro.TextMeshProUGUI>();
            tmpAr.text = "العربية";
            tmpAr.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontsRoot + "/Amiri-Bold SDF.asset");
            tmpAr.fontSize = 13f;
            tmpAr.color = ChkobbaPalette.Hex(0xFFF6EA);
            tmpAr.alignment = TMPro.TextAlignmentOptions.Center;
            tmpAr.raycastTarget = false;
            Stretch((RectTransform)arLabel.transform);

            NewIconButton(seg, "French", null, AssetsRoot + "/seg-off.png", new Vector2(80, 40),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-41f, 0f), Color.white);
            var frLabel = new GameObject("Label", typeof(RectTransform));
            frLabel.transform.SetParent(seg.transform.Find("French"), false);
            var tmpFr = frLabel.AddComponent<TMPro.TextMeshProUGUI>();
            tmpFr.text = "Français";
            tmpFr.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontsRoot + "/Amiri-Bold SDF.asset");
            tmpFr.fontSize = 13f;
            tmpFr.color = ChkobbaPalette.CobaltDeep;
            tmpFr.alignment = TMPro.TextAlignmentOptions.Center;
            tmpFr.raycastTarget = false;
            Stretch((RectTransform)frLabel.transform);
        }

        // ---- helpers -----------------------------------------------------------

        private static Image NewImage(GameObject parent, string name, string spritePath, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            if (!string.IsNullOrEmpty(spritePath)) img.sprite = Load<Sprite>(spritePath);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// Button with bg sprite + optional stretched icon child. Returns the bg image.
        private static Image NewIconButton(GameObject parent, string name, string iconPath, string bgPath,
            Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 pos, Color iconColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            if (!string.IsNullOrEmpty(bgPath))
            {
                img.sprite = Load<Sprite>(bgPath);
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            if (!string.IsNullOrEmpty(iconPath))
            {
                var icon = NewImage(go, "Icon", iconPath, iconColor);
                Stretch((RectTransform)icon.transform, 10f, 10f);
            }
            return img;
        }

        private static TMPro.TextMeshProUGUI NewText(GameObject parent, string name, string content,
            string fontPath, float size, Color color, float letterSpacing = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);
            if (font != null) tmp.font = font;
            else Debug.LogWarning("[Chkobba] font asset not found: " + fontPath);
            tmp.text = content;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            if (letterSpacing != 0f) tmp.characterSpacing = letterSpacing;
            return tmp;
        }

        /// Top-anchored full-width strip: y = distance from top, h = height.
        private static void FullStretchTop(RectTransform rt, float y, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -y);
            rt.sizeDelta = new Vector2(0f, h);
        }

        /// Bottom strip: spans from the canvas bottom up to the given height.
        private static void FullStretchBottom(RectTransform rt, float height)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static void Stretch(RectTransform rt, float padX = 0f, float padY = 0f)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        private static void SetField(object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (f != null) f.SetValue(target, value);
            else Debug.LogWarning("[Chkobba] field not found: " + field + " on " + target.GetType().Name);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning("[Chkobba] missing asset: " + path);
            return asset;
        }
    }
}
#endif
