﻿using Dalamud.Game.Text.SeStringHandling.Payloads;
using LiteDB;
using MapPartyAssist.Types.Attributes;

namespace MapPartyAssist.Types {
    [ValidatedDataType]
    public class MPAMapLink {
        public int RawX { get; set; }
        public int RawY { get; set; }
        public uint TerritoryTypeId { get; set; }
        public uint MapId { get; set; }

        [BsonIgnore]
        private readonly MapLinkPayload? _mapLinkPayload;

        [BsonCtor]
        public MPAMapLink() {
        }

        public MPAMapLink(uint territoryTypeId, uint mapId, int rawX, int rawY) {
            RawX = rawX;
            RawY = rawY;
            TerritoryTypeId = territoryTypeId;
            MapId = mapId;
            _mapLinkPayload = new MapLinkPayload(territoryTypeId, mapId, rawX, rawY);
        }

        public MPAMapLink(MapLinkPayload mapLinkPayload) {
            RawX = mapLinkPayload.RawX;
            RawY = mapLinkPayload.RawY;
            TerritoryTypeId = mapLinkPayload.TerritoryType.RowId;
            MapId = mapLinkPayload.Map.RowId;
            _mapLinkPayload = mapLinkPayload;
        }

        public MapLinkPayload GetMapLinkPayload() {
            return _mapLinkPayload ?? new MapLinkPayload(TerritoryTypeId, MapId, RawX, RawY);
        }
    }
}
