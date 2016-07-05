using System.Text.RegularExpressions;

namespace Mina.Example.Haiku
{
    class PhraseUtilities
    {
        public static int CountSyllablesInPhrase(string phrase)
        {
            var syllables = 0;

            var regex = new Regex("[^\\w-]+");

            foreach (var word in regex.Split(phrase))
            {
                if (word.Length > 0)
                {
                    syllables += CountSyllablesInWord(word.ToLower());
                }
            }

            return syllables;
        }

        static int CountSyllablesInWord(string word)
        {
            var chars = word.ToCharArray();
            var syllables = 0;
            var lastWasVowel = false;

            for (var i = 0; i < chars.Length; i++)
            {
                var c = chars[i];
                if (IsVowel(c))
                {
                    if (!lastWasVowel
                            || (i > 0 && IsE(chars, i - 1) && IsO(chars, i)))
                    {
                        ++syllables;
                        lastWasVowel = true;
                    }
                }
                else
                {
                    lastWasVowel = false;
                }
            }

            if (word.EndsWith("oned") || word.EndsWith("ne")
                    || word.EndsWith("ide") || word.EndsWith("ve")
                    || word.EndsWith("fe") || word.EndsWith("nes")
                    || word.EndsWith("mes"))
            {
                --syllables;
            }

            return syllables;
        }

        static bool IsE(char[] chars, int position)
        {
            return IsCharacter(chars, position, 'e');
        }

        static bool IsCharacter(char[] chars, int position, char c)
        {
            return chars[position] == c;
        }

        static bool IsO(char[] chars, int position)
        {
            return IsCharacter(chars, position, 'o');
        }

        static bool IsVowel(char c)
        {
            return c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u'
                    || c == 'y';
        }
    }
}
