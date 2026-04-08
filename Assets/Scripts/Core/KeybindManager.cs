using System;
using System.Collections.Generic;
using UnityEngine;

namespace MimicFacility.Core
{
    [Serializable]
    public class Keybind
    {
        public string actionName;
        public KeyCode key;

        public Keybind(string actionName, KeyCode key)
        {
            this.actionName = actionName;
            this.key = key;
        }
    }

    [Serializable]
    public class KeybindProfile
    {
        public List<Keybind> bindings = new List<Keybind>();
    }

    public class KeybindManager : MonoBehaviour
    {
        private const string PrefsKey = "KeybindProfile";

        public static KeybindManager Instance { get; private set; }

        public event Action<string, KeyCode> OnKeybindChanged;

        private KeybindProfile _profile = new KeybindProfile();
        private readonly Dictionary<string, Keybind> _lookup = new Dictionary<string, Keybind>();

        public IReadOnlyList<Keybind> AllBindings => _profile.bindings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (!Load())
                ResetToDefaults();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ResetToDefaults()
        {
            _profile = new KeybindProfile();
            _profile.bindings = new List<Keybind>
            {
                new Keybind("MoveForward", KeyCode.W),
                new Keybind("MoveLeft", KeyCode.A),
                new Keybind("MoveBackward", KeyCode.S),
                new Keybind("MoveRight", KeyCode.D),
                new Keybind("Sprint", KeyCode.LeftShift),
                new Keybind("Jump", KeyCode.Space),
                new Keybind("Interact", KeyCode.E),
                new Keybind("UseGear", KeyCode.Mouse0),
                new Keybind("Flashlight", KeyCode.F),
                new Keybind("PushToTalk", KeyCode.V),
                new Keybind("Pause", KeyCode.Escape),
                new Keybind("Minimap", KeyCode.M),
                new Keybind("Scoreboard", KeyCode.Tab)
            };

            RebuildLookup();
            Save();
        }

        public void RebindKey(string actionName, KeyCode newKey)
        {
            if (!_lookup.TryGetValue(actionName, out Keybind bind))
            {
                Debug.LogWarning($"KeybindManager: Unknown action '{actionName}'");
                return;
            }

            KeyCode oldKey = bind.key;
            bind.key = newKey;

            Save();
            OnKeybindChanged?.Invoke(actionName, newKey);
        }

        public KeyCode GetKey(string actionName)
        {
            if (_lookup.TryGetValue(actionName, out Keybind bind))
                return bind.key;

            Debug.LogWarning($"KeybindManager: Unknown action '{actionName}'");
            return KeyCode.None;
        }

        public bool IsPressed(string actionName)
        {
            return Input.GetKey(GetKey(actionName));
        }

        public bool WasPressed(string actionName)
        {
            return Input.GetKeyDown(GetKey(actionName));
        }

        public bool HasConflict(string actionName)
        {
            if (!_lookup.TryGetValue(actionName, out Keybind bind))
                return false;

            for (int i = 0; i < _profile.bindings.Count; i++)
            {
                Keybind other = _profile.bindings[i];
                if (other.actionName != actionName && other.key == bind.key)
                    return true;
            }
            return false;
        }

        public List<string> GetConflicts(KeyCode key)
        {
            var conflicts = new List<string>();
            for (int i = 0; i < _profile.bindings.Count; i++)
            {
                if (_profile.bindings[i].key == key)
                    conflicts.Add(_profile.bindings[i].actionName);
            }
            return conflicts;
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_profile, false);
                PlayerPrefs.SetString(PrefsKey, json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"KeybindManager: Failed to save keybinds: {e.Message}");
            }
        }

        private bool Load()
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
                return false;

            try
            {
                string json = PlayerPrefs.GetString(PrefsKey);
                JsonUtility.FromJsonOverwrite(json, _profile);

                if (_profile.bindings == null || _profile.bindings.Count == 0)
                    return false;

                RebuildLookup();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"KeybindManager: Failed to load keybinds: {e.Message}");
                return false;
            }
        }

        private void RebuildLookup()
        {
            _lookup.Clear();
            for (int i = 0; i < _profile.bindings.Count; i++)
            {
                Keybind bind = _profile.bindings[i];
                _lookup[bind.actionName] = bind;
            }
        }
    }
}
