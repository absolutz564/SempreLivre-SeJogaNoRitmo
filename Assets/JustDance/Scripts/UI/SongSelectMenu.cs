using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu de seleção de música. Exibido antes de PlayerSelectModal.
/// Salva a coreografia escolhida em GameSession.SelectedChoreography e passa para o modal de jogadores.
/// </summary>
public class SongSelectMenu : MonoBehaviour
{
    [System.Serializable]
    public struct SongEntry
    {
        public string            songName;
        public DanceChoreography choreography;
        public Sprite            coverSprite;
    }

    [Header("Músicas")]
    public SongEntry[] songs;              // [0]=Piseiro  [1]=Forró  [2]=Xote

    [Header("Botões de seleção")]
    public Button[] songButtons;           // um botão por música

    [Header("UI por carta")]
    public Image[]           coverImages;
    public TextMeshProUGUI[] nameLabels;

    [Header("Próximo passo")]
    public PlayerSelectModal playerSelectModal;

    void Awake()
    {
        for (int i = 0; i < songButtons.Length; i++)
        {
            int idx = i;
            songButtons[i]?.onClick.AddListener(() => Select(idx));
        }
    }

    void OnEnable() => RefreshUI();

    public void Show() => gameObject.SetActive(true);

    void RefreshUI()
    {
        for (int i = 0; i < songs.Length; i++)
        {
            if (i < coverImages.Length && coverImages[i] != null)
                coverImages[i].sprite = songs[i].coverSprite;
            if (i < nameLabels.Length && nameLabels[i] != null)
                nameLabels[i].text = songs[i].songName;
        }
    }

    void Select(int index)
    {
        if (index < 0 || index >= songs.Length) return;
        GameSession.SelectedChoreography = songs[index].choreography;
        gameObject.SetActive(false);
        playerSelectModal?.Show();
    }
}
