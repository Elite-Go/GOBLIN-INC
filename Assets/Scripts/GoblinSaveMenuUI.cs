using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// Unity UI Button의 OnClick에 연결할 메서드를 제공한다.
    /// 시작 화면: 불러오기 또는 새 게임 선택 / 인게임: 1~3번 슬롯 저장.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoblinSaveMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GoblinSaveManager saveManager;

        [Header("Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject loadSlotPanel;
        [SerializeField] private GameObject gameplayWorldRoot;
        [SerializeField] private GameObject gameplayUiRoot;
        [SerializeField] private GameObject saveSlotPanel;

        [Header("Load Slot Buttons (index 0 = slot 1)")]
        [SerializeField] private Button[] loadSlotButtons = new Button[3];
        [SerializeField] private TextMeshProUGUI[] loadSlotLabels = new TextMeshProUGUI[3];

        [Header("Save Slot Labels (index 0 = slot 1)")]
        [SerializeField] private TextMeshProUGUI[] saveSlotLabels = new TextMeshProUGUI[3];

        [Header("Optional Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackText;

        private void Start()
        {
            // SaveManager / Roster / Spawner는 gameplayWorldRoot 바깥의 상시 활성 오브젝트에 둔다.
            if (gameplayWorldRoot != null)
                gameplayWorldRoot.SetActive(false);

            if (gameplayUiRoot != null)
                gameplayUiRoot.SetActive(false);

            if (loadSlotPanel != null)
                loadSlotPanel.SetActive(false);

            if (saveSlotPanel != null)
                saveSlotPanel.SetActive(false);

            if (titlePanel != null)
                titlePanel.SetActive(true);

            RefreshSlotUI();
            SetFeedback(string.Empty);
        }

        public void OnClickOpenLoadPanel()
        {
            RefreshSlotUI();
            SetPanel(titlePanel, false);
            SetPanel(loadSlotPanel, true);
        }

        public void OnClickBackToTitle()
        {
            SetPanel(loadSlotPanel, false);
            SetPanel(titlePanel, true);
            SetFeedback(string.Empty);
        }

        public void OnClickNewGame()
        {
            if (saveManager == null)
            {
                SetFeedback("GoblinSaveManager가 연결되지 않았습니다.");
                return;
            }

            if (!saveManager.TryStartNewGame(out string error))
            {
                SetFeedback(error);
                return;
            }

            EnterGameplay();
            SetFeedback("새 게임을 시작했습니다.");
        }

        public void OnClickLoadSlot1() { LoadSlot(1); }
        public void OnClickLoadSlot2() { LoadSlot(2); }
        public void OnClickLoadSlot3() { LoadSlot(3); }

        public void OnClickSaveSlot1() { SaveSlot(1); }
        public void OnClickSaveSlot2() { SaveSlot(2); }
        public void OnClickSaveSlot3() { SaveSlot(3); }

        public void OnClickOpenSavePanel()
        {
            RefreshSlotUI();
            SetPanel(saveSlotPanel, true);
        }

        public void OnClickCloseSavePanel()
        {
            SetPanel(saveSlotPanel, false);
        }

        public void RefreshSlotUI()
        {
            for (int slot = 1; slot <= GoblinSaveManager.SlotCount; slot++)
            {
                bool exists = false;
                string displayText = $"슬롯 {slot}";
                string error = null;
                bool infoValid = saveManager != null &&
                    saveManager.TryGetSlotInfo(slot, out exists, out displayText, out error);

                int index = slot - 1;
                if (loadSlotLabels != null && index < loadSlotLabels.Length && loadSlotLabels[index] != null)
                    loadSlotLabels[index].text = infoValid ? displayText : $"슬롯 {slot} - 확인 오류";

                if (loadSlotButtons != null && index < loadSlotButtons.Length && loadSlotButtons[index] != null)
                    loadSlotButtons[index].interactable = infoValid && exists;

                if (saveSlotLabels != null && index < saveSlotLabels.Length && saveSlotLabels[index] != null)
                    saveSlotLabels[index].text = infoValid ? displayText : $"슬롯 {slot} - 저장 가능";
            }
        }

        private void LoadSlot(int slot)
        {
            if (saveManager == null)
            {
                SetFeedback("GoblinSaveManager가 연결되지 않았습니다.");
                return;
            }

            if (!saveManager.TryLoadSlot(slot, out string error))
            {
                SetFeedback(error);
                RefreshSlotUI();
                return;
            }

            EnterGameplay();
            SetFeedback(string.IsNullOrEmpty(error)
                ? $"슬롯 {slot}을 불러왔습니다."
                : $"슬롯 {slot}을 불러왔습니다. 단, 일부 고블린 소환에 문제가 있습니다. {error}");
            RefreshSlotUI();
        }

        private void SaveSlot(int slot)
        {
            if (saveManager == null)
            {
                SetFeedback("GoblinSaveManager가 연결되지 않았습니다.");
                return;
            }

            if (!saveManager.TrySaveSlot(slot, out string error))
            {
                SetFeedback(error);
                return;
            }

            SetFeedback($"슬롯 {slot}에 저장했습니다.");
            RefreshSlotUI();
        }

        private void EnterGameplay()
        {
            SetPanel(titlePanel, false);
            SetPanel(loadSlotPanel, false);
            SetPanel(gameplayWorldRoot, true);
            SetPanel(gameplayUiRoot, true);
            SetPanel(saveSlotPanel, false);
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message ?? string.Empty;
        }

        private static void SetPanel(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}
