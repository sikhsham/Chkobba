using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Chkobba
{
    /// <summary>
    /// Arabic text shaping for TextMeshPro (this TMP build has no Arabic shaping engine).
    /// Converts base Arabic letters (0621-064A) into presentation forms (FE70-FEFF) with
    /// contextual joining + lam-alef ligatures, then reorders each line visually so mixed
    /// Arabic/Latin/digit strings render correctly without TMP's RTL flag.
    ///
    /// One instance per TMP text (attached at scene build): the first time a shape is
    /// requested it checks the text's own font character table and falls back to the base
    /// letter for any presentation form the font doesn't ship (e.g. FE8D, which several
    /// Arabic fonts omit because it renders identically to the base alef 0627).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ArabicTextShaper : MonoBehaviour, TMPro.ITextPreprocessor
    {
        [SerializeField] private TMPro.TMP_Text target; // injected at build time / Awake

        // glyph forms per letter: [isolated, final, initial, medial] (0 = not available)
        private static readonly Dictionary<char, int[]> Forms = new Dictionary<char, int[]>
        {
            {'\u0621', new[]{0xFE80,0,0,0}},          // hamza: non-joining
            {'\u0622', new[]{0xFE81,0xFE82,0,0}},     // alef madda
            {'\u0623', new[]{0xFE83,0xFE84,0,0}},     // alef hamza above
            {'\u0624', new[]{0xFE85,0xFE86,0,0}},     // waw hamza
            {'\u0625', new[]{0xFE87,0xFE88,0,0}},     // alef hamza below
            {'\u0626', new[]{0xFE89,0xFE8A,0xFE8B,0xFE8C}}, // yeh hamza
            {'\u0627', new[]{0xFE8D,0xFE8E,0,0}},     // alef
            {'\u0628', new[]{0xFE8F,0xFE90,0xFE91,0xFE92}}, // beh
            {'\u0629', new[]{0xFE93,0xFE94,0,0}},     // teh marbuta
            {'\u062A', new[]{0xFE95,0xFE96,0xFE97,0xFE98}}, // teh
            {'\u062B', new[]{0xFE99,0xFE9A,0xFE9B,0xFE9C}}, // theh
            {'\u062C', new[]{0xFE9D,0xFE9E,0xFE9F,0xFEA0}}, // jeem
            {'\u062D', new[]{0xFEA1,0xFEA2,0xFEA3,0xFEA4}}, // hah
            {'\u062E', new[]{0xFEA5,0xFEA6,0xFEA7,0xFEA8}}, // khah
            {'\u062F', new[]{0xFEA9,0xFEAA,0,0}},     // dal
            {'\u0630', new[]{0xFEAB,0xFEAC,0,0}},     // thal
            {'\u0631', new[]{0xFEAD,0xFEAE,0,0}},     // reh
            {'\u0632', new[]{0xFEAF,0xFEB0,0,0}},     // zain
            {'\u0633', new[]{0xFEB1,0xFEB2,0xFEB3,0xFEB4}}, // seen
            {'\u0634', new[]{0xFEB5,0xFEB6,0xFEB7,0xFEB8}}, // sheen
            {'\u0635', new[]{0xFEB9,0xFEBA,0xFEBB,0xFEBC}}, // sad
            {'\u0636', new[]{0xFEBD,0xFEBE,0xFEBF,0xFEC0}}, // dad
            {'\u0637', new[]{0xFEC1,0xFEC2,0xFEC3,0xFEC4}}, // tah
            {'\u0638', new[]{0xFEC5,0xFEC6,0xFEC7,0xFEC8}}, // zah
            {'\u0639', new[]{0xFEC9,0xFECA,0xFECB,0xFECC}}, // ain
            {'\u063A', new[]{0xFECD,0xFECE,0xFECF,0xFED0}}, // ghain
            {'\u0641', new[]{0xFED1,0xFED2,0xFED3,0xFED4}}, // feh
            {'\u0642', new[]{0xFED5,0xFED6,0xFED7,0xFED8}}, // qaf
            {'\u0643', new[]{0xFED9,0xFEDA,0xFEDB,0xFEDC}}, // kaf
            {'\u0644', new[]{0xFEDD,0xFEDE,0xFEDF,0xFEE0}}, // lam
            {'\u0645', new[]{0xFEE1,0xFEE2,0xFEE3,0xFEE4}}, // meem
            {'\u0646', new[]{0xFEE5,0xFEE6,0xFEE7,0xFEE8}}, // noon
            {'\u0647', new[]{0xFEE9,0xFEEA,0xFEEB,0xFEEC}}, // heh
            {'\u0648', new[]{0xFEED,0xFEEE,0,0}},     // waw
            {'\u0649', new[]{0xFEEF,0xFEF0,0,0}},     // alef maksura
            {'\u064A', new[]{0xFEF1,0xFEF2,0xFEF3,0xFEF4}}, // yeh
        };

        // lam-alef ligatures: lam + alef variant -> [isolated, final]
        private static readonly Dictionary<char, int[]> LamAlef = new Dictionary<char, int[]>
        {
            {'\u0622', new[]{0xFEF5,0xFEF6}},
            {'\u0623', new[]{0xFEF7,0xFEF8}},
            {'\u0625', new[]{0xFEF9,0xFEFA}},
            {'\u0627', new[]{0xFEFB,0xFEFC}},
        };

        private static readonly HashSet<char> DualJoining = new HashSet<char>
        {
            '\u0626','\u0628','\u062A','\u062B','\u062C','\u062D','\u062E',
            '\u0633','\u0634','\u0635','\u0636','\u0637','\u0638','\u0639','\u063A',
            '\u0641','\u0642','\u0643','\u0644','\u0645','\u0646','\u0647','\u064A','\u0640'
        };

        private HashSet<int> _available;   // codepoints this text's font can render
        private bool _checked;

        private static bool IsArabicLetter(char c) { return c >= '\u0621' && c <= '\u064A'; }        private static bool IsArabicDiacritic(char c) { return c >= '\u064B' && c <= '\u065F'; }
        private static bool IsArabicBlock(char c) { return c >= '\u0600' && c <= '\u06FF'; }
        private static bool IsPresentationForm(char c) { return (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'); }

        private static bool IsRtlChar(char c)
        {
            return IsArabicBlock(c) || IsPresentationForm(c) || c == '\u061B' || c == '\u061F' || c == '\u060C'
                || c == '\u00AB' || c == '\u00BB' || c == '!' || c == '?' || c == '\u2014' || c == '\u00B7';
        }

        private static bool IsLtrChar(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.'
                || (c >= '\u00C0' && c <= '\u00FF' && c != '\u00D7' && c != '\u00F7');
        }

        private void Awake()
        {
            if (target == null) target = GetComponent<TMPro.TMP_Text>();
            if (target != null) target.textPreprocessor = this;
        }

        /// <summary>Editor-time helper used by the scene builder: attach + set target.</summary>
        public void Configure(TMPro.TMP_Text text)
        {
            target = text;
            text.textPreprocessor = this;
        }

        public string PreprocessText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!ContainsArabic(text)) return text;
            EnsureAvailabilityCheck();
            return ShapeAndReorder(text);
        }

        private void EnsureAvailabilityCheck()
        {
            if (_checked) return;
            _checked = true;
            _available = null;
            var font = target != null ? target.font : null;
            if (font != null && font.characterTable != null)
            {
                _available = new HashSet<int>();
                foreach (var ch in font.characterTable)
                    _available.Add((int)ch.unicode);
            }
        }


        private static bool ContainsArabic(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (IsArabicLetter(s[i]) || IsPresentationForm(s[i])) return true;
            return false;
        }

        // ---- shaping: logical Arabic -> presentation forms -------------------

        private string Shape(string text)
        {
            var sb = new StringBuilder(text.Length + 8);
            bool prevConnects = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (IsArabicDiacritic(c)) { sb.Append(c); continue; } // transparent to joining

                if (c == '\u0644' && i + 1 < text.Length && LamAlef.ContainsKey(text[i + 1]))
                {
                    int[] lig = LamAlef[text[i + 1]];
                    int code = prevConnects ? lig[1] : lig[0];
                    sb.Append((char)Pick(code, text[i + 1]));  // fallback: keep the bare alef
                    prevConnects = false;                       // ligature is right-joining only
                    i++;
                    continue;
                }

                if (IsArabicLetter(c) && Forms.TryGetValue(c, out int[] forms))
                {
                    char next = PeekNextLetter(text, i);
                    bool nextConnectsBack = next != '\0' && next != '\u0621';

                    bool dual = DualJoining.Contains(c);
                    int code;
                    if (dual)
                    {
                        if (prevConnects && nextConnectsBack) code = forms[3];
                        else if (prevConnects) code = forms[1];
                        else if (nextConnectsBack) code = forms[2];
                        else code = forms[0];
                    }
                    else
                    {
                        code = prevConnects && forms[1] != 0 ? forms[1] : forms[0];
                    }
                    sb.Append((char)Pick(code, c)); // fallback: base letter
                    prevConnects = dual;
                    continue;
                }

                sb.Append(c);
                prevConnects = false;
            }
            return sb.ToString();
        }

        /// <summary>Returns the presentation form if the font actually ships it, else the base letter.</summary>
        private int Pick(int presentationForm, char baseLetter)
        {
            if (presentationForm == 0) return baseLetter;
            if (_available == null) return presentationForm; // no table: assume present, let TMP warn
            return _available.Contains(presentationForm) ? presentationForm : baseLetter;
        }

        private static char PeekNextLetter(string text, int i)
        {
            for (int j = i + 1; j < text.Length; j++)
            {
                if (IsArabicDiacritic(text[j])) continue;
                return IsArabicLetter(text[j]) ? text[j] : '\0';
            }
            return '\0';
        }

        // ---- visual reorder: RTL runs reversed, LTR runs kept -----------------

        private string ShapeAndReorder(string text)
        {
            string shaped = Shape(text);
            var sb = new StringBuilder(shaped.Length);
            var lines = shaped.Split('\n');
            for (int li = 0; li < lines.Length; li++)
            {
                sb.Append(ReorderLine(lines[li]));
                if (li < lines.Length - 1) sb.Append('\n');
            }
            return sb.ToString();
        }

        private static string ReorderLine(string line)
        {
            var runs = new List<KeyValuePair<bool, StringBuilder>>();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == ' ' || c == '\u00A0' || c == '\t')
                {
                    if (runs.Count == 0) { Append(runs, true, c); continue; }
                    runs[runs.Count - 1].Value.Append(c);
                    continue;
                }
                bool isRtl = IsRtlChar(c);
                bool isLtr = IsLtrChar(c);
                if (!isRtl && !isLtr) { Append(runs, runs.Count > 0 ? runs[runs.Count - 1].Key : true, c); continue; }
                Append(runs, isRtl, c);
            }

            var outp = new StringBuilder(line.Length);
            for (int r = runs.Count - 1; r >= 0; r--)
            {
                var sb = runs[r].Value;
                if (runs[r].Key)
                {
                    for (int i = sb.Length - 1; i >= 0; i--) outp.Append(sb[i]);
                }
                else outp.Append(sb);
            }
            return outp.ToString();
        }

        private static void Append(List<KeyValuePair<bool, StringBuilder>> runs, bool rtl, char c)
        {
            if (runs.Count > 0 && runs[runs.Count - 1].Key == rtl) runs[runs.Count - 1].Value.Append(c);
            else runs.Add(new KeyValuePair<bool, StringBuilder>(rtl, new StringBuilder().Append(c)));
        }
    }
}
