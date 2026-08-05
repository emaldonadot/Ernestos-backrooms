using System.Collections.Generic;

namespace EndlessRooms.Procedural
{
    /// <summary>Inputs to <see cref="RoomGraphGenerator"/>. A plain POCO so tests can construct it without a scene.</summary>
    public sealed class RoomGraphGenerationSettings
    {
        public int Seed { get; set; }

        /// <summary>Target total room count, including the entry and exit rooms. Generation may fall a little short if the grid runs out of compatible free cells.</summary>
        public int RoomCount { get; set; } = 10;

        public RoomDefinition EntryDefinition { get; set; }

        public RoomDefinition ExitDefinition { get; set; }

        /// <summary>Definitions used to fill the critical path and branches (never the entry or exit definition).</summary>
        public IReadOnlyList<RoomDefinition> FillerDefinitions { get; set; }
    }
}
