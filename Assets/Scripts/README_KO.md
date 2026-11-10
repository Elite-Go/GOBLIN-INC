# 고블린 세이브 시스템 - 3개 슬롯 + UI 연결

기존 `GoblinPhase1_Pooling` 구조에 세이브/로드 기능을 추가한 버전입니다.

## 구현 내용

- 1~3번, 총 3개의 독립 JSON 세이브 슬롯
- 인게임 UI 버튼으로 원하는 슬롯에 저장
- 시작 화면에서 `불러오기` 또는 `새 게임` 선택
- 불러오기 선택 후 저장 데이터가 있는 슬롯 중 하나를 선택
- 고블린의 `InstanceId`, 종류 ID, 레벨, 현재 체력, MapId, 마지막 위치 저장
- 저장된 고유 ID를 그대로 유지한 채 복원
- 불러온 맵과 일치하는 고블린만 Object Pool에서 꺼내 재소환
- 다른 맵에 있는 고블린 데이터도 Roster에 유지
- 새 게임은 현재 런타임 소유 목록만 초기화하며 기존 세이브 파일은 삭제하지 않음
- 새 게임 시작 고블린 종류/수/초기 위치를 Inspector에서 설정 가능

> 현재 버전에는 장비 데이터가 아직 없으므로 장비 저장은 포함되지 않습니다. 장비 시스템을 추가할 때 `GoblinSaveRecord`에도 장비 ID 목록을 함께 추가하면 됩니다.

---

## 1. 파일 구성

기존 풀링 버전 파일에 다음 스크립트가 추가/수정되었습니다.

### 추가 파일

- `GoblinSaveManager.cs` : 3개 슬롯의 JSON 저장/불러오기, 새 게임 초기화, 맵별 고블린 재소환
- `GoblinSaveMenuUI.cs` : Unity UI Button의 OnClick에 연결할 함수 제공

### 수정 파일

- `GoblinInstance.cs` : 기존 InstanceId를 유지한 채 세이브 데이터에서 복원하는 `Restore` 추가
- `GoblinRoster.cs` : 로스터 전체 교체 `TryReplaceAll`, 전체 초기화 `ClearAll` 추가

나머지 풀링 관련 파일은 기존 버전을 그대로 사용합니다.

---

## 2. Unity에 스크립트 배치

모든 `.cs` 파일을 같은 폴더에 두어도 됩니다.

```text
Assets/
└── Scripts/
    └── Goblins/
        ├── GoblinBaseStats.cs
        ├── GoblinDefinition.cs
        ├── GoblinDatabase.cs
        ├── GoblinInstance.cs
        ├── GoblinRoster.cs
        ├── GoblinActor.cs
        ├── IGoblinPoolable.cs
        ├── GoblinActorPool.cs
        ├── GoblinSpawner.cs
        ├── GoblinSaveManager.cs
        └── GoblinSaveMenuUI.cs
```

기존 풀링 버전에서 `GoblinInstance.cs`, `GoblinRoster.cs`는 반드시 이번 압축 파일의 수정본으로 교체해야 합니다.

---

## 3. 씬 오브젝트 구성

중요: `GoblinRoster`, `GoblinActorPool`, `GoblinSpawner`, `GoblinSaveManager`는 시작 메뉴에서 게임 월드를 비활성화하더라도 계속 살아 있어야 합니다. 따라서 `GameplayWorldRoot`의 자식으로 넣지 말고 씬의 상위 관리자 오브젝트에 배치합니다.

권장 Hierarchy 예시:

```text
Scene
├── GameSystems                     [항상 활성]
│   ├── GoblinRoster
│   ├── GoblinActorPool
│   ├── GoblinSpawner
│   └── GoblinSaveManager
│
├── GameplayWorldRoot               [시작 화면에서 비활성]
│   ├── Map
│   ├── GoblinActorRoot              <- GoblinSpawner의 Active Actor Root
│   └── OtherWorldObjects
│
└── Canvas                           [항상 활성]
    ├── TitlePanel
    │   ├── LoadGameButton
    │   └── NewGameButton
    ├── LoadSlotPanel                [처음에는 비활성]
    │   ├── LoadSlot1Button
    │   ├── LoadSlot2Button
    │   ├── LoadSlot3Button
    │   └── BackButton
    ├── GameplayUIRoot               [시작 화면에서 비활성]
    │   └── OpenSavePanelButton
    ├── SaveSlotPanel                [처음에는 비활성]
    │   ├── SaveSlot1Button
    │   ├── SaveSlot2Button
    │   ├── SaveSlot3Button
    │   └── CloseSavePanelButton
    └── FeedbackText
```

Canvas는 계속 활성 상태로 두고, `TitlePanel`, `LoadSlotPanel`, `GameplayUIRoot`처럼 화면 단위 Panel만 켜고 끕니다. `GoblinSaveMenuUI` 컴포넌트는 항상 활성 상태인 Canvas 또는 별도 UI 관리자 오브젝트에 부착하세요.

### GameSystems 컴포넌트 연결

1. `GoblinRoster`의 Database에 기존 `GoblinDatabase` 에셋을 연결합니다.
2. `GoblinActorPool`을 같은 `GameSystems` 오브젝트 또는 별도 항상 활성 오브젝트에 추가합니다.
3. `GoblinSpawner`에 Roster와 ActorPool을 연결합니다.
4. GoblinSpawner의 `Active Actor Root`에는 `GameplayWorldRoot/GoblinActorRoot`를 연결합니다.
5. `GoblinSpawner`의 `Default Map Id`와 `GoblinSaveManager`의 `Initial Map Id`는 동일한 값(예: `home`)으로 설정합니다.
6. `GoblinSaveManager`에 Roster와 Spawner를 연결합니다.

`GoblinSaveManager`는 `Application.persistentDataPath/GoblinSaves/` 폴더를 자동 생성합니다. 파일은 다음과 같이 생성됩니다.

```text
GoblinSaves/
├── slot_1.json
├── slot_2.json
└── slot_3.json
```

이 경로는 에디터 프로젝트 폴더가 아니라 기기별 영구 데이터 경로입니다. 실제 경로는 `GoblinSaveManager.GetSlotFilePath(1)` 등으로 확인할 수 있습니다.

---

## 4. 새 게임 시작 고블린 설정

`GoblinSaveManager` Inspector의 `New Game - Starting Goblins` 목록에서 시작 고블린을 설정합니다.

각 항목의 의미:

- `Definition Id`: GoblinDatabase에 등록된 종류 ID. 예: `goblin.common`
- `Count`: 시작 시 생성할 마릿수
- `Start Position`: 첫 번째 고블린의 월드 좌표
- `Spacing`: 다음 고블린마다 더할 좌표 간격

예시:

```text
Element 0
  Definition Id = goblin.common
  Count = 3
  Start Position = (0, 0)
  Spacing = (1.2, 0)

Element 1
  Definition Id = goblin.warrior
  Count = 1
  Start Position = (4, 0)
  Spacing = (1, 0)
```

위 예시는 새 게임 시작 시 일반 고블린 3마리와 전사 고블린 1마리를 `home` 맵에 생성합니다. 목록을 비워두면 고블린이 없는 상태로 시작합니다.

새 게임은 기존 `slot_1.json`~`slot_3.json`을 삭제하지 않습니다. 새 게임 데이터를 보존하려면 사용자가 저장 버튼으로 슬롯을 선택해 저장해야 합니다.

---

## 5. UI Button OnClick 연결

`GoblinSaveMenuUI`를 Canvas에 부착하고 Inspector에서 다음 항목을 연결합니다.

### Panels

- `Save Manager` : GameSystems의 GoblinSaveManager
- `Title Panel` : 시작 화면 Panel
- `Load Slot Panel` : 저장 슬롯을 고르는 Panel
- `Gameplay World Root` : 실제 월드 오브젝트 부모
- `Gameplay UI Root` : 게임 중 표시할 HUD 부모
- `Save Slot Panel` : 인게임 저장 슬롯 Panel

### Load Slot Buttons / Labels

배열 크기를 3으로 설정하고 다음 순서로 연결합니다.

- Element 0 = 슬롯 1 버튼 / 슬롯 1 설명 Text
- Element 1 = 슬롯 2 버튼 / 슬롯 2 설명 Text
- Element 2 = 슬롯 3 버튼 / 슬롯 3 설명 Text

로드 버튼은 해당 슬롯에 정상적인 세이브가 있을 때만 활성화됩니다. 각 설명 Text에는 저장 날짜와 고블린 수가 표시됩니다.

### Save Slot Labels

저장 패널의 슬롯별 Text 3개를 같은 순서로 연결합니다. 저장 후 날짜와 고블린 수가 갱신됩니다.

### 버튼별 OnClick 함수

Button의 `OnClick()` 이벤트에서 Canvas에 붙은 `GoblinSaveMenuUI` 오브젝트를 드래그한 다음 해당 함수를 선택합니다.

| UI 버튼 | 연결할 함수 |
|---|---|
| 시작 화면 - 불러오기 | `GoblinSaveMenuUI.OnClickOpenLoadPanel()` |
| 시작 화면 - 새 게임 | `GoblinSaveMenuUI.OnClickNewGame()` |
| 불러오기 - 슬롯 1 | `GoblinSaveMenuUI.OnClickLoadSlot1()` |
| 불러오기 - 슬롯 2 | `GoblinSaveMenuUI.OnClickLoadSlot2()` |
| 불러오기 - 슬롯 3 | `GoblinSaveMenuUI.OnClickLoadSlot3()` |
| 불러오기 - 뒤로 | `GoblinSaveMenuUI.OnClickBackToTitle()` |
| 게임 중 - 저장 패널 열기 | `GoblinSaveMenuUI.OnClickOpenSavePanel()` |
| 저장 - 슬롯 1 | `GoblinSaveMenuUI.OnClickSaveSlot1()` |
| 저장 - 슬롯 2 | `GoblinSaveMenuUI.OnClickSaveSlot2()` |
| 저장 - 슬롯 3 | `GoblinSaveMenuUI.OnClickSaveSlot3()` |
| 저장 패널 닫기 | `GoblinSaveMenuUI.OnClickCloseSavePanel()` |

`Feedback Text`를 연결하면 저장/불러오기 성공 및 오류 메시지가 표시됩니다. Text는 기본 Unity UI의 `UnityEngine.UI.Text`를 사용합니다. TextMeshPro를 사용 중이라면 UI 스크립트의 `Text` 타입을 `TMP_Text`로 변경하고 `using TMPro;`를 추가하면 됩니다.

---

## 6. 실제 실행 흐름

### 앱 시작

1. `GoblinSaveMenuUI.Start()`가 월드와 게임 HUD를 비활성화합니다.
2. TitlePanel만 표시합니다.
3. 3개 슬롯 파일을 검사해 슬롯 버튼의 활성/비활성 및 정보를 갱신합니다.
4. 자동으로 가장 최근 세이브를 불러오지는 않습니다. 사용자가 불러오기 또는 새 게임을 선택합니다.

### 불러오기

1. 불러오기 버튼을 누릅니다.
2. 저장 데이터가 있는 슬롯 버튼이 활성화됩니다.
3. 슬롯 1~3 중 하나를 선택합니다.
4. JSON 전체를 읽고 포맷, 고블린 ID, 종류 ID, 좌표 등을 검증합니다.
5. 현재 Actor를 Object Pool에 반환합니다.
6. Roster의 고블린 목록을 저장 당시 데이터로 교체합니다.
7. 저장된 현재 맵과 같은 MapId를 가진 살아있는 고블린만 풀에서 대여해 다시 소환합니다.
8. 월드 및 HUD를 표시합니다.

다른 맵에 저장된 고블린은 Roster 데이터에 남고 현재 씬에는 소환되지 않습니다. 이후 맵 전환 시 `TrySpawnSavedGoblinsForMap("원하는맵ID", out int count, out string error)`를 호출하면 해당 맵의 고블린을 소환할 수 있습니다.

### 새 게임

1. 현재 화면 Actor를 풀에 반환합니다.
2. 현재 Roster 데이터를 비웁니다.
3. `Initial Map Id`로 현재 맵을 초기화합니다.
4. Inspector의 Starting Goblins 설정에 맞춰 새 고블린을 생성합니다.
5. 기존 세이브 파일은 보존합니다.

### 저장

1. 인게임에서 저장 패널을 엽니다.
2. 1~3번 슬롯 중 하나를 누릅니다.
3. 활성 Actor의 현재 Transform 좌표를 GoblinInstance에 반영합니다.
4. Roster에 있는 모든 고블린을 JSON으로 직렬화합니다.
5. 선택한 슬롯 파일만 덮어씁니다. 나머지 두 슬롯은 변경하지 않습니다.

---

## 7. 저장되는 데이터

현재 구현 기준으로 고블린마다 다음 정보가 저장됩니다.

| 필드 | 설명 |
|---|---|
| `instanceId` | 개별 고블린 고유 ID. 불러와도 유지 |
| `definitionId` | GoblinDefinition 에셋을 다시 찾기 위한 ID |
| `level` | 개별 고블린 레벨 |
| `currentHealth` | 개별 현재 체력 |
| `mapId` | 마지막으로 존재한 맵 ID |
| `positionX`, `positionY` | 마지막 월드 좌표 |

`GoblinDefinition`의 ScriptableObject 에셋이나 Prefab 자체를 JSON으로 저장하지 않습니다. 불러올 때 `definitionId`로 GoblinDatabase에서 원본 Definition을 다시 찾습니다.

현재 버전은 체력을 Definition의 기본 최대 체력 이하로 보정해 복원합니다. 나중에 개별 최대 체력 강화/장비 보정이 추가되면 최종 최대 체력 계산식을 SaveManager에도 연결해야 합니다.

---

## 8. 스크립트에서 직접 호출할 때

```csharp
// 1번 슬롯 저장
if (!saveManager.TrySaveSlot(1, out string saveError))
    Debug.LogError(saveError);

// 2번 슬롯 불러오기
if (!saveManager.TryLoadSlot(2, out string loadError))
    Debug.LogError(loadError);

// 새 게임 시작
if (!saveManager.TryStartNewGame(out string newGameError))
    Debug.LogError(newGameError);
```

---

## 9. 테스트 순서

1. Inspector에서 `Starting Goblins`에 일반 고블린 2마리를 설정합니다.
2. Play를 누릅니다. 시작 화면이 나오고 월드는 숨겨져 있어야 합니다.
3. 새 게임을 누릅니다. 일반 고블린 2마리가 시작 좌표에 생성되는지 확인합니다.
4. 두 고블린 중 하나를 다른 좌표로 옮기고, 한 마리의 체력을 변경합니다.
5. 슬롯 1 저장 버튼을 누릅니다.
6. Play를 종료한 뒤 다시 실행합니다.
7. 불러오기 → 슬롯 1을 선택합니다.
8. 고블린 수, 각 고블린의 위치, 체력, InstanceId가 저장 당시와 같은지 확인합니다.
9. 슬롯 2에 저장하고 슬롯 1과 독립적으로 관리되는지 확인합니다.
10. 새 게임을 시작한 뒤 슬롯 1, 2의 세이브 파일이 여전히 남아 있는지 확인합니다.

세이브 파일은 `Application.persistentDataPath/GoblinSaves/` 아래에 저장됩니다. Unity Editor에서는 다음 코드로 경로를 확인할 수 있습니다.

```csharp
Debug.Log(Application.persistentDataPath);
```

---

## 10. 현재 범위와 주의사항

- 세이브 슬롯은 3개로 고정되어 있습니다.
- 저장은 버튼을 눌렀을 때만 수행하며 자동 저장은 아직 없습니다.
- 현재 구현은 고블린 개체 데이터와 위치를 저장합니다. 월드 타일 변경, 건축물, 자원, 장비, 퀘스트 데이터는 아직 포함하지 않습니다.
- 세이브 포맷 버전은 `formatVersion = 1`입니다. 저장 데이터 구조가 바뀌면 버전 업 및 마이그레이션 처리를 추가해야 합니다.
- 실제 Unity Editor에서 Inspector 참조 연결, UI OnClick 연결, 플레이 모드 저장/복원 테스트가 필요합니다. 이 제작 환경에는 Unity Editor가 없어 Unity 엔진 내 컴파일/플레이 테스트까지 완료했다고 보장하지 않습니다.
