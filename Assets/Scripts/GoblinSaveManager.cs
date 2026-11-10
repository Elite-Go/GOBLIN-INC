using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// 고블린 소유 데이터를 1~3번 JSON 슬롯에 저장하고 복원한다.
    /// GoblinInstance의 고유 ID는 저장/복원 과정에서 유지된다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoblinSaveManager : MonoBehaviour
    {
        public const int SlotCount = 3;
        private const int CurrentFormatVersion = 1;
        private const string SaveFolderName = "GoblinSaves";

        [Header("References")]
        [SerializeField] private GoblinRoster roster;
        [SerializeField] private GoblinSpawner spawner;

        [Header("World")]
        [SerializeField] private string initialMapId = "home";
        [SerializeField] private string currentMapId = "home";

        [Header("New Game - Starting Goblins")]
        [Tooltip("새 게임 시작 시 생성할 기본 고블린 목록입니다. 비워두면 고블린 없이 시작합니다.")]
        [SerializeField] private List<StartingGoblin> startingGoblins = new List<StartingGoblin>();

        public string CurrentMapId => currentMapId;
        public bool HasActiveSession { get; private set; }
        public string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, SaveFolderName);

        [Serializable]
        public sealed class StartingGoblin
        {
            [SerializeField] private string definitionId = "goblin.common";
            [SerializeField, Min(1)] private int count = 1;
            [SerializeField] private Vector2 startPosition;
            [SerializeField] private Vector2 spacing = new Vector2(1f, 0f);

            public string DefinitionId => definitionId;
            public int Count => Mathf.Max(1, count);
            public Vector2 StartPosition => startPosition;
            public Vector2 Spacing => spacing;
        }

        [Serializable]
        public sealed class SaveFileData
        {
            public int formatVersion = CurrentFormatVersion;
            public string savedAtUtc;
            public string mapId;
            public List<GoblinSaveRecord> goblins = new List<GoblinSaveRecord>();
        }

        [Serializable]
        public sealed class GoblinSaveRecord
        {
            public string instanceId;
            public string definitionId;
            public int level;
            public float currentHealth;
            public string mapId;
            public float positionX;
            public float positionY;
        }

        private void OnValidate()
        {
            initialMapId = string.IsNullOrWhiteSpace(initialMapId) ? "home" : initialMapId.Trim();
            currentMapId = string.IsNullOrWhiteSpace(currentMapId) ? initialMapId : currentMapId.Trim();
        }

        /// <summary>지정한 슬롯에 현재 소유 고블린 전체를 저장한다. 슬롯 번호는 1~3이다.</summary>
        public bool TrySaveSlot(int slot, out string error)
        {
            error = null;

            if (!ValidateReferences(out error) || !ValidateSlot(slot, out error))
                return false;

            if (!HasActiveSession)
            {
                error = "게임 세션이 시작되지 않았습니다. 먼저 새 게임을 시작하거나 세이브를 불러오세요.";
                return false;
            }

            try
            {
                // 풀에 대여된 Actor의 최신 Transform 좌표를 GoblinInstance로 복사한다.
                spawner.CaptureActivePositions();

                SaveFileData saveData = new SaveFileData
                {
                    formatVersion = CurrentFormatVersion,
                    savedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                    mapId = currentMapId,
                    goblins = new List<GoblinSaveRecord>(roster.Count)
                };

                IReadOnlyList<GoblinInstance> owned = roster.OwnedGoblins;
                for (int i = 0; i < owned.Count; i++)
                {
                    GoblinInstance goblin = owned[i];
                    if (goblin == null)
                        continue;

                    Vector2 position = goblin.SavedPosition;
                    saveData.goblins.Add(new GoblinSaveRecord
                    {
                        instanceId = goblin.InstanceId,
                        definitionId = goblin.DefinitionId,
                        level = goblin.Level,
                        currentHealth = goblin.CurrentHealth,
                        mapId = goblin.MapId,
                        positionX = position.x,
                        positionY = position.y
                    });
                }

                string json = JsonUtility.ToJson(saveData, true);
                Directory.CreateDirectory(SaveDirectoryPath);

                string path = GetSlotPath(slot);
                string temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

                // 기존 파일을 먼저 지우지 않고 새 파일을 완성한 뒤 교체한다.
                if (File.Exists(path))
                {
                    string backupPath = path + ".bak";
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    File.Move(path, backupPath);
                    try
                    {
                        File.Move(temporaryPath, path);
                    }
                    catch
                    {
                        if (!File.Exists(path) && File.Exists(backupPath))
                            File.Move(backupPath, path);
                        throw;
                    }

                    // 새 슬롯 파일이 정상 배치된 뒤 백업을 지운다.
                    // 백업 삭제에 실패하더라도 새 저장 자체는 성공한 것으로 취급한다.
                    try
                    {
                        if (File.Exists(backupPath))
                            File.Delete(backupPath);
                    }
                    catch (Exception cleanupException)
                    {
                        Debug.LogWarning($"세이브 백업 파일 정리 실패: {cleanupException.Message}", this);
                    }
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                return true;
            }
            catch (Exception exception)
            {
                error = $"슬롯 {slot} 저장에 실패했습니다: {exception.Message}";
                Debug.LogException(exception, this);
                return false;
            }
        }

        /// <summary>슬롯의 데이터를 복원하고 현재 맵에 해당하는 살아있는 고블린을 소환한다.</summary>
        public bool TryLoadSlot(int slot, out string error)
        {
            error = null;

            if (!ValidateReferences(out error) || !ValidateSlot(slot, out error))
                return false;

            SaveFileData saveData;
            List<GoblinInstance> restoredGoblins;

            // 파일을 완전히 읽고 검증한 다음에만 현재 게임 데이터를 교체한다.
            if (!TryReadAndBuildSaveData(slot, out saveData, out restoredGoblins, out error))
                return false;

            spawner.DespawnAll();

            if (!roster.TryReplaceAll(restoredGoblins, out error))
                return false;

            currentMapId = string.IsNullOrWhiteSpace(saveData.mapId) ? "home" : saveData.mapId;
            HasActiveSession = true;

            int failedSpawnCount = SpawnSavedGoblinsForCurrentMapInternal(currentMapId, out string spawnError);
            if (failedSpawnCount > 0)
            {
                // 데이터 복원은 성공했으므로 저장 데이터를 버리지는 않는다.
                error = $"세이브 데이터는 복원했지만 {failedSpawnCount}마리의 화면 소환에 실패했습니다. {spawnError}";
            }

            return true;
        }

        /// <summary>
        /// 새 게임을 시작한다. 현재 런타임 고블린은 제거하지만 기존 3개 세이브 파일은 삭제하지 않는다.
        /// Starting Goblins 목록을 Inspector에서 설정하면 시작 고블린도 생성한다.
        /// </summary>
        public bool TryStartNewGame(out string error)
        {
            error = null;

            if (!ValidateReferences(out error))
                return false;

            spawner.DespawnAll();
            roster.ClearAll();
            currentMapId = initialMapId;
            HasActiveSession = true;

            if (startingGoblins == null)
                return true;

            for (int groupIndex = 0; groupIndex < startingGoblins.Count; groupIndex++)
            {
                StartingGoblin group = startingGoblins[groupIndex];
                if (group == null || string.IsNullOrWhiteSpace(group.DefinitionId))
                    continue;

                for (int i = 0; i < group.Count; i++)
                {
                    Vector2 position = group.StartPosition + group.Spacing * i;
                    Vector3 worldPosition = new Vector3(position.x, position.y, 0f);

                    if (!spawner.TryCreateAndSpawn(
                        group.DefinitionId,
                        currentMapId,
                        worldPosition,
                        out _,
                        out error))
                    {
                        // 시작 고블린 중 하나라도 실패하면 부분 시작 상태를 남기지 않는다.
                        spawner.DespawnAll();
                        roster.ClearAll();
                        HasActiveSession = false;
                        error = $"새 게임 시작 고블린 생성에 실패했습니다. {error}";
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 맵 전환 시 호출한다. 이전 맵의 Actor를 풀에 반환하고 새 맵에 저장된 고블린을 다시 소환한다.
        /// 해당 맵이 아닌 고블린 데이터는 Roster에 그대로 남는다.
        /// </summary>
        public bool TrySpawnSavedGoblinsForMap(string mapId, out int spawnedCount, out string error)
        {
            spawnedCount = 0;
            error = null;

            if (!ValidateReferences(out error))
                return false;

            if (string.IsNullOrWhiteSpace(mapId))
            {
                error = "맵 ID는 비어 있을 수 없습니다.";
                return false;
            }

            spawner.DespawnAll();
            currentMapId = mapId.Trim();
            int failures = SpawnSavedGoblinsForCurrentMapInternal(currentMapId, out error);
            spawnedCount = spawner.ActiveActorCount;
            return failures == 0;
        }

        /// <summary>슬롯에 저장 데이터가 있는지와 UI에 표시할 정보를 반환한다.</summary>
        public bool TryGetSlotInfo(int slot, out bool exists, out string displayText, out string error)
        {
            exists = false;
            displayText = string.Empty;
            error = null;

            if (!ValidateSlot(slot, out error))
                return false;

            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                displayText = $"슬롯 {slot} - 비어 있음";
                return true;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                SaveFileData data = JsonUtility.FromJson<SaveFileData>(json);
                if (data == null || data.formatVersion != CurrentFormatVersion || data.goblins == null)
                {
                    displayText = $"슬롯 {slot} - 손상되었거나 지원하지 않는 세이브";
                    error = "세이브 파일의 형식이 올바르지 않습니다.";
                    return false;
                }

                exists = true;
                string dateText = "날짜 정보 없음";
                if (DateTime.TryParse(data.savedAtUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime savedAt))
                {
                    dateText = savedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
                }

                displayText = $"슬롯 {slot} - {dateText} / 고블린 {data.goblins.Count}마리";
                return true;
            }
            catch (Exception exception)
            {
                displayText = $"슬롯 {slot} - 읽기 오류";
                error = exception.Message;
                return false;
            }
        }

        /// <summary>실제 저장 파일 경로를 반환한다. 디버깅용으로 사용할 수 있다.</summary>
        public string GetSlotFilePath(int slot)
        {
            if (!ValidateSlot(slot, out _))
                return string.Empty;

            return GetSlotPath(slot);
        }

        private int SpawnSavedGoblinsForCurrentMapInternal(string mapId, out string error)
        {
            int failedCount = 0;
            StringBuilder failures = new StringBuilder();
            IReadOnlyList<GoblinInstance> owned = roster.OwnedGoblins;

            for (int i = 0; i < owned.Count; i++)
            {
                GoblinInstance goblin = owned[i];
                if (goblin == null || !goblin.IsAlive ||
                    !string.Equals(goblin.MapId, mapId, StringComparison.Ordinal))
                    continue;

                if (!spawner.TrySpawnSaved(goblin.InstanceId, mapId, out _, out string spawnError))
                {
                    failedCount++;
                    if (failures.Length > 0)
                        failures.Append(" | ");
                    failures.Append(goblin.InstanceId).Append(": ").Append(spawnError);
                }
            }

            error = failures.ToString();
            return failedCount;
        }

        private bool TryReadAndBuildSaveData(
            int slot,
            out SaveFileData saveData,
            out List<GoblinInstance> restoredGoblins,
            out string error)
        {
            saveData = null;
            restoredGoblins = null;
            error = null;

            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                error = $"슬롯 {slot}에 저장된 데이터가 없습니다.";
                return false;
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                saveData = JsonUtility.FromJson<SaveFileData>(json);

                if (saveData == null || saveData.formatVersion != CurrentFormatVersion || saveData.goblins == null)
                {
                    error = "세이브 파일이 비어 있거나 지원하지 않는 형식입니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(saveData.mapId))
                    saveData.mapId = "home";

                HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
                restoredGoblins = new List<GoblinInstance>(saveData.goblins.Count);

                for (int i = 0; i < saveData.goblins.Count; i++)
                {
                    GoblinSaveRecord record = saveData.goblins[i];
                    if (record == null || string.IsNullOrWhiteSpace(record.instanceId) ||
                        string.IsNullOrWhiteSpace(record.definitionId) || string.IsNullOrWhiteSpace(record.mapId))
                    {
                        error = $"세이브 파일의 {i}번째 고블린 데이터가 유효하지 않습니다.";
                        return false;
                    }

                    if (!ids.Add(record.instanceId))
                    {
                        error = $"세이브 파일에 중복된 고블린 ID가 있습니다: {record.instanceId}";
                        return false;
                    }

                    if (!roster.Database.TryGetDefinition(record.definitionId, out GoblinDefinition definition) || definition == null)
                    {
                        error = $"등록되지 않은 고블린 종류입니다: {record.definitionId}";
                        return false;
                    }

                    if (!IsFinite(record.currentHealth) || record.currentHealth < 0f ||
                        !IsFinite(record.positionX) || !IsFinite(record.positionY))
                    {
                        error = $"세이브 파일의 능력치 또는 좌표가 유효하지 않습니다: {record.instanceId}";
                        return false;
                    }

                    float safeHealth = Mathf.Min(record.currentHealth, definition.BaseStats.MaxHealth);
                    GoblinInstance restored = GoblinInstance.Restore(
                        record.instanceId,
                        record.definitionId,
                        record.level,
                        safeHealth,
                        record.mapId,
                        new Vector2(record.positionX, record.positionY));

                    restoredGoblins.Add(restored);
                }

                return true;
            }
            catch (Exception exception)
            {
                error = $"슬롯 {slot} 파일을 읽거나 해석하지 못했습니다: {exception.Message}";
                Debug.LogException(exception, this);
                return false;
            }
        }

        private bool ValidateReferences(out string error)
        {
            if (roster == null)
            {
                error = "GoblinSaveManager에 GoblinRoster가 연결되지 않았습니다.";
                return false;
            }

            if (spawner == null)
            {
                error = "GoblinSaveManager에 GoblinSpawner가 연결되지 않았습니다.";
                return false;
            }

            if (roster.Database == null)
            {
                error = "GoblinRoster에 GoblinDatabase가 연결되지 않았습니다.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool ValidateSlot(int slot, out string error)
        {
            if (slot < 1 || slot > SlotCount)
            {
                error = $"세이브 슬롯은 1~{SlotCount}번만 사용할 수 있습니다.";
                return false;
            }

            error = null;
            return true;
        }

        private string GetSlotPath(int slot)
        {
            return Path.Combine(SaveDirectoryPath, $"slot_{slot}.json");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
