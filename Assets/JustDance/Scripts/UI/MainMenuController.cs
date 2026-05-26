using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [System.Serializable]
    public struct SelectableButton
    {
        public Button button;
        public Sprite normalSprite;
        public Sprite selectedSprite;
    }

    [Header("Ritmos — ordem: Xote, Piseiro, Galope")]
    public DanceChoreography[]  rhythmChoreographies;
    public SelectableButton[]   rhythmButtons;          // um por ritmo, cada um com seu sprite

    [Header("Modos")]
    public SelectableButton btn1Player;
    public SelectableButton btn2Players;

    [Header("Iniciar")]
    public Button btnStart;

    [Header("Referência")]
    public DanceController danceController;

    private int _selectedRhythm = -1;
    private int _selectedMode   =  0;

    void Awake()
    {
        for (int i = 0; i < rhythmButtons.Length; i++)
        {
            int idx = i;
            rhythmButtons[i].button?.onClick.AddListener(() => SelectRhythm(idx));
        }
        btn1Player .button?.onClick.AddListener(() => SelectMode(1));
        btn2Players.button?.onClick.AddListener(() => SelectMode(2));
        btnStart           ?.onClick.AddListener(StartGame);
    }

    void OnEnable()
    {
        _selectedRhythm = -1;
        _selectedMode   =  0;
        RefreshUI();
    }

    void SelectRhythm(int index) { _selectedRhythm = index; RefreshUI(); }
    void SelectMode(int mode)    { _selectedMode   = mode;  RefreshUI(); }

    void RefreshUI()
    {
        for (int i = 0; i < rhythmButtons.Length; i++)
            Apply(ref rhythmButtons[i], i == _selectedRhythm);

        Apply(ref btn1Player,  _selectedMode == 1);
        Apply(ref btn2Players, _selectedMode == 2);

        if (btnStart != null)
            btnStart.interactable = _selectedRhythm >= 0 && _selectedMode > 0;
    }

    static void Apply(ref SelectableButton sb, bool selected)
    {
        if (sb.button == null) return;
        var img = sb.button.targetGraphic as Image;
        if (img == null) return;
        img.sprite = selected ? sb.selectedSprite : sb.normalSprite;
    }

    void StartGame()
    {
        if (_selectedRhythm >= 0 && _selectedRhythm < rhythmChoreographies.Length)
            GameSession.SelectedChoreography = rhythmChoreographies[_selectedRhythm];
        GameSession.PlayerCount = _selectedMode;
        gameObject.SetActive(false);
        danceController?.StartDance();
    }

    public void Show() => gameObject.SetActive(true);
}
