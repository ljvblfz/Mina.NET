using System;

namespace Mina.Example.Haiku
{
    class Haiku
    {
        public Haiku(params string[] lines)
        {
            if (lines == null || lines.Length != 3)
                throw new ArgumentException("Must pass in 3 phrases of text");
            Phrases = lines;
        }

        public string[] Phrases { get; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;

            var haiku = obj as Haiku;
            if (haiku == null)
                return false;

            if (Phrases.Length != haiku.Phrases.Length)
                return false;

            for (var i = 0; i < Phrases.Length; i++)
            {
                if (!string.Equals(Phrases[i], haiku.Phrases[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var result = 1;

            foreach (var s in Phrases)
                result = 31 * result + (s == null ? 0 : s.GetHashCode());

            return result;
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", Phrases) + "]";
        }
    }
}
