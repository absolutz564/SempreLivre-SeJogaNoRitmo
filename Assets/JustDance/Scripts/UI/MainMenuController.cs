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
    public SelectableButton btnStart;

    [Header("Referência")]
    public DanceController danceController;

    private int _selectedRhythm = -1;
    private int _selectedMode   =  0;

    // Navegação por passador de slide (PageDown / PageUp)
    // Seção 0 = Ritmo, 1 = Modo, 2 = Iniciar
    private int _navSection = 0;
    private int _navIndex   = 0;

    void Awake()
    {
        for (int i = 0; i < rhythmButtons.Length; i++)
        {
            int idx = i;
            rhythmButtons[i].button?.onClick.AddListener(() => SelectRhythm(idx));
        }
        btn1Player .button?.onClick.AddListener(() => SelectMode(1));
        btn2Players.button?.onClick.AddListener(() => SelectMode(2));
        btnStart.button    ?.onClick.AddListener(StartGame);
    }

    void OnEnable()
    {
        _navSection = 0;
        _navIndex   = 0;
        _selectedMode = 0;
        SelectRhythm(0); // pré-seleciona primeiro ritmo
    }

    void Update()
    {
        if (!Input.anyKeyDown) return;

        bool pageDown = Input.GetKeyDown(KeyCode.PageDown);
        bool pageUp   = Input.GetKeyDown(KeyCode.PageUp);

        if (_navSection == 2)
        {
            // Qualquer tecla inicia o jogo
            StartGame();
            return;
        }

        if (pageDown || pageUp)
        {
            NavigateCurrent(pageDown ? 1 : -1);
        }
        else
        {
            AdvanceSection();
        }
    }

    void NavigateCurrent(int dir)
    {
        if (_navSection == 0)
        {
            _navIndex = Mathf.Clamp(_navIndex + dir, 0, rhythmButtons.Length - 1);
            SelectRhythm(_navIndex);
        }
        else if (_navSection == 1)
        {
            _navIndex = Mathf.Clamp(_navIndex + dir, 0, 1);
            SelectMode(_navIndex + 1); // índice 0 → 1 jogador, 1 → 2 jogadores
        }
    }

    void AdvanceSection()
    {
        _navSection++;
        _navIndex = 0;
        if (_navSection == 1)
            SelectMode(1); // pré-seleciona 1 Jogador ao entrar na seção de modo
        else if (_navSection == 2)
            RefreshUI();   // aplica feedback visual no botão Iniciar
    }

    void SelectRhythm(int index) { _selectedRhythm = index; RefreshUI(); }
    void SelectMode(int mode)    { _selectedMode   = mode;  RefreshUI(); }

    void RefreshUI()
    {
        for (int i = 0; i < rhythmButtons.Length; i++)
            Apply(ref rhythmButtons[i], i == _selectedRhythm);

        Apply(ref btn1Player,  _selectedMode == 1);
        Apply(ref btn2Players, _selectedMode == 2);
        Apply(ref btnStart,    _navSection == 2);

        if (btnStart.button != null)
            btnStart.button.interactable = _selectedRhythm >= 0 && _selectedMode > 0;
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
        if (_selectedRhythm < 0 || _selectedMode == 0) return;
        if (_selectedRhythm < rhythmChoreographies.Length)
            GameSession.SelectedChoreography = rhythmChoreographies[_selectedRhythm];
        GameSession.PlayerCount = _selectedMode;
        gameObject.SetActive(false);
        danceController?.StartDance();
    }

    public void Show() => gameObject.SetActive(true);
}
