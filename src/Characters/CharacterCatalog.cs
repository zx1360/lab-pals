using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace LabPals
{
    // Generated CharacterData comes from assets/characters.json at build time.
    public sealed class CharacterDefinition
    {
        public Character Id { get; private set; }
        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public string English { get; private set; }
        public string Traits { get; private set; }
        public string ReactionCaption { get; private set; }
        public Color Accent { get; private set; }
        public PointF LeftWrist { get; private set; }
        public PointF RightWrist { get; private set; }

        internal CharacterDefinition(Character id, string name, string shortName, string english,
            string traits, string reactionCaption, int accent, float lx, float ly, float rx, float ry)
        {
            Id = id; Name = name; ShortName = shortName; English = english;
            Traits = traits; ReactionCaption = reactionCaption;
            Accent = Color.FromArgb((accent >> 16) & 255, (accent >> 8) & 255, accent & 255);
            LeftWrist = new PointF(lx, ly); RightWrist = new PointF(rx, ry);
        }
    }

    public static class CharacterCatalog
    {
        public static readonly ReadOnlyCollection<CharacterDefinition> All =
            Array.AsReadOnly(CharacterData.Create());
        private static readonly Dictionary<Character, CharacterDefinition> byId = CreateIndex();

        private static Dictionary<Character, CharacterDefinition> CreateIndex()
        {
            var index = new Dictionary<Character, CharacterDefinition>();
            foreach (var item in All) index.Add(item.Id, item);
            return index;
        }
        public static bool Contains(Character id) { return byId.ContainsKey(id); }
        public static CharacterDefinition Get(Character id)
        {
            CharacterDefinition item;
            if (!byId.TryGetValue(id, out item)) throw new ArgumentOutOfRangeException("id");
            return item;
        }
    }
}
