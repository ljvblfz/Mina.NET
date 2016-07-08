namespace Mina.Example.Haiku
{
    class HaikuValidator
    {
        private static readonly int[] SyllableCounts = { 5, 7, 5 };

        public void Validate(Haiku haiku)
        {
            var phrases = haiku.Phrases;

            for (var i = 0; i < phrases.Length; i++)
            {
                var phrase = phrases[i];
                var count = PhraseUtilities.CountSyllablesInPhrase(phrase);

                if (count != SyllableCounts[i])
                {
                    throw new InvalidHaikuException(i + 1, phrase, count,
                            SyllableCounts[i]);
                }
            }
        }
    }
}
