using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string sceneName;
    public string saveLabel;
    public long totalPlaytimeSeconds;
    public long realWorldUnixSeconds;
    public string playerName;

    // ✅ per-slot selected character index (NOT PlayerPrefs)
    public int selectedCharacterIndex = 0;

    // ✅ objective save payload (JSON)
    public string objectivePayloadJson = "";

    // ✅ rewards/badges save payload (JSON) ------------- NEW
    public string rewardPayloadJson = "";

    // ------------- PLAYER -------------
    [Serializable]
    public class PlayerSnapshot
    {
        public float px, py, pz;
        public float rx, ry, rz;
    }
    public PlayerSnapshot player;

    // ------------- HOST (NEW) -------------
    [Serializable]
    public class HostSnapshot
    {
        public float px, py, pz;
        public float rx, ry, rz;
    }

    // ✅ NEW: saved host pose
    public HostSnapshot host;

    // ------------- OBJECTS -------------
    [Serializable]
    public class ObjectSnapshot
    {
        public string id;      // SaveID.ID
        public string payload; // JSON for one MultiPayload
    }

    public List<ObjectSnapshot> objects = new List<ObjectSnapshot>();
}