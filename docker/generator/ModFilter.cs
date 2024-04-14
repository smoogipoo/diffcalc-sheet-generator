// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using osu.Game.Online.API;

namespace Generator
{
    public class ModFilter
    {
        private readonly List<string> includedMods = new List<string>();
        private readonly List<string> excludedMods = new List<string>();
        private readonly List<string> onlyMods = new List<string>();

        public ModFilter(string filter)
        {
            char? type = null;
            StringBuilder acronym = new StringBuilder();

            foreach (char c in filter)
            {
                switch (c)
                {
                    case '-':
                    case '+':
                    case '=':
                        if (type != null)
                            commit();

                        type = c;
                        break;

                    default:
                        acronym.Append(c);
                        break;
                }
            }

            commit();

            void commit()
            {
                if (type == null)
                    return;

                switch (type)
                {
                    case '-':
                        excludedMods.Add(acronym.ToString());
                        break;

                    case '+':
                        includedMods.Add(acronym.ToString());
                        break;

                    case '=':
                        onlyMods.Add(acronym.ToString());
                        break;
                }

                type = null;
                acronym.Clear();
            }
        }

        /// <summary>
        /// Whether the given mods match this filter.
        /// </summary>
        /// <param name="mods">The mods.</param>
        public bool Matches(APIMod[] mods)
        {
            foreach (string check in excludedMods)
            {
                if (mods.Any(m => matches(m, check)))
                    return false;
            }

            foreach (string check in includedMods)
            {
                if (mods.All(m => !matches(m, check)))
                    return false;
            }

            if (onlyMods.Count > 0 && mods.Length != onlyMods.Count)
                return false;

            foreach (string check in onlyMods)
            {
                if (mods.All(m => !matches(m, check)))
                    return false;
            }

            return true;
        }

        private static bool matches(APIMod mod, string acronym)
        {
            return baseAcronym(mod.Acronym) == baseAcronym(acronym);

            static string baseAcronym(string acronym) => acronym switch
            {
                "NC" => "DT",
                "DC" => "HT",
                _ => acronym
            };
        }
    }
}
