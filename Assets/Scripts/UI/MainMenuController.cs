using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chkobba
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Sound chip")]
        [SerializeField] public Image chipIcon;
        [SerializeField] public Sprite soundOnIcon;
        [SerializeField] public Sprite mutedIcon;

        [Header("Feedback")]
        [SerializeField] public Toast toast;
        [SerializeField] public RankingCard rankingCard;
        [SerializeField] public PlayCardsAnimator playAnimator;

        [Header("Sheets")]
        [SerializeField] public SheetPanel rulesSheet;
        [SerializeField] public SheetPanel settingsSheet;
        [SerializeField] public CanvasGroup backdrop;

        [Header("Settings sheet")]
        [SerializeField] public Image soundToggle;
        [SerializeField] public Image musicToggle;
        [SerializeField] public Sprite toggleOn;
        [SerializeField] public Sprite toggleOff;
        [SerializeField] public Image langArabic;
        [SerializeField] public Image langFrench;
        [SerializeField] public Sprite segOnSprite;
        [SerializeField] public Sprite segOffSprite;

        [Header("Coming soon strings")]
        [SerializeField] private string friendsToast = "العب مع صحابك على نفس التيليفون — قريب في التحديث الجاي!";
        [SerializeField] private string profileToast = "ملفك ونتائجك مع صحابك — قادمين قريب.";
        [SerializeField] private string rankingToast = "الترتيب مازال ما تحلّش — قريب إن شاء الله!";
        [SerializeField] private string dealMessage = "نحضّرو الطاولة…";

        private bool soundOn = true;
        private bool musicOn = true;
        private bool startingGame;

        public bool SoundOn => soundOn;
        public bool MusicOn => musicOn;

        private void Start()
        {
            AutoWire();
            if (toast != null) toast.Hide();
            if (backdrop != null) { backdrop.alpha = 0f; backdrop.blocksRaycasts = false; }
            if (chipIcon != null && soundOnIcon != null) chipIcon.sprite = soundOnIcon;
            ApplyLang(langArabic);
        }

        /// <summary>Binds scene buttons to handlers by hierarchy path.</summary>
        private void AutoWire()
        {
            Bind("SoundChip", OnSoundChip);
            Bind("SettingsChip", OnSettings);
            Bind("Fan/Rules", OnRules);
            Bind("Fan/Play", OnPlay);
            Bind("Fan/Friends", OnFriends);
            Bind("Fan/Profile", OnProfile);
            Bind("Backdrop", OnBackdropClicked);
            Bind("SettingsSheet/Body/SoundRow/Toggle", OnSoundToggle);
            Bind("SettingsSheet/Body/MusicRow/Toggle", OnMusicToggle);
            Bind("SettingsSheet/Body/LangRow/Seg/Arabic", OnLangArabic);
            Bind("SettingsSheet/Body/LangRow/Seg/French", OnLangFrench);

            var rc = transform.Find("RulesSheet/Header/Close");
            if (rc != null && rulesSheet != null) BindButton(rc.gameObject, rulesSheet.Close);
            var sc = transform.Find("SettingsSheet/Header/Close");
            if (sc != null && settingsSheet != null) BindButton(sc.gameObject, settingsSheet.Close);
        }

        private void Bind(string path, Action action)
        {
            var t = transform.Find(path);
            if (t == null) { Debug.LogWarning("[Chkobba] auto-wire miss: " + path); return; }
            BindButton(t.gameObject, action);
        }

        private static void BindButton(GameObject go, Action action)
        {
            var b = go.GetComponent<Button>();
            if (b == null) b = go.AddComponent<Button>();
            b.onClick.AddListener(new UnityEngine.Events.UnityAction(action));
        }

        // ---- chip / direct actions ---------------------------------------

        public void OnSoundChip()
        {
            soundOn = !soundOn;
            if (chipIcon != null && soundOnIcon != null && mutedIcon != null)
                chipIcon.sprite = soundOn ? soundOnIcon : mutedIcon;
            if (soundToggle != null && toggleOn != null && toggleOff != null)
                soundToggle.sprite = soundOn ? toggleOn : toggleOff;
            if (toast != null) toast.Show(soundOn ? "الصوت رجع — تسمع القهوة؟" : "الصوت مكتوم");
        }

        public void OnSettings() { if (settingsSheet != null) settingsSheet.Open(this); }

        public void OnRules() { if (rulesSheet != null) rulesSheet.Open(this); }

        public void OnFriends() { toast.Show(friendsToast); }

        public void OnProfile() { toast.Show(profileToast); }

        public void OnRanking()
        {
            if (rankingCard != null) rankingCard.Shake();
            toast.Show(rankingToast);
        }

        // ---- play ---------------------------------------------------------

        public void OnPlay()
        {
            if (startingGame) return;
            startingGame = true;
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            if (playAnimator != null) yield return playAnimator.CollectAndDeal(dealMessage, toast);
            SceneManager.LoadScene(gameSceneName);
        }

        // ---- sheets -------------------------------------------------------

        public void NotifySheetOpened(SheetPanel sheet)
        {
            if (backdrop == null) return;
            backdrop.alpha = 1f;
            backdrop.blocksRaycasts = true;
        }

        public void NotifySheetClosed(SheetPanel sheet)
        {
            if (backdrop == null) return;
            backdrop.alpha = 0f;
            backdrop.blocksRaycasts = false;
        }

        public void CloseTopSheet()
        {
            if (rulesSheet != null && rulesSheet.IsOpen) rulesSheet.Close();
            else if (settingsSheet != null && settingsSheet.IsOpen) settingsSheet.Close();
        }

        public void OnBackdropClicked() { CloseTopSheet(); }

        // ---- settings sheet ----------------------------------------------

        public void OnSoundToggle()
        {
            soundOn = !soundOn;
            if (soundToggle != null && toggleOn != null && toggleOff != null)
                soundToggle.sprite = soundOn ? toggleOn : toggleOff;
        }

        public void OnMusicToggle()
        {
            musicOn = !musicOn;
            if (musicToggle != null && toggleOn != null && toggleOff != null)
                musicToggle.sprite = musicOn ? toggleOn : toggleOff;
        }

        public void OnLangArabic() { ApplyLang(langArabic); toast.Show("رجّعناها عربي — بروفة"); }

        public void OnLangFrench() { ApplyLang(langFrench); toast.Show("Le français arrive bientôt — بروفة"); }

        private void ApplyLang(Image active)
        {
            if (segOnSprite == null || segOffSprite == null) return;
            if (langArabic != null) langArabic.sprite = active == langArabic ? segOnSprite : segOffSprite;
            if (langFrench != null) langFrench.sprite = active == langFrench ? segOnSprite : segOffSprite;
        }

        private void Update()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Back button closes an open sheet first (Input System: Escape == Android back).
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) CloseTopSheet();
#endif
        }
    }
}
