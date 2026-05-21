using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal exibido antes do jogo para escolher 1 ou 2 jogadores.
/// Salva a escolha em GameSession.PlayerCount e dispara DanceController.StartDance().
/// </summary>
public class PlayerSelectModal : MonoBehaviour
{
    [Header("Referências")]
    public DanceController danceController;

    [Header("Botões")]
    public Button btn1Player;
    public Button btn2Players;

    void Awake()
    {
        btn1Player?.onClick.AddListener(() => Select(1));
        btn2Players?.onClick.AddListener(() => Select(2));
    }

    public void Show() => gameObject.SetActive(true);

    void Select(int count)
    {
        GameSession.PlayerCount = count;
        gameObject.SetActive(false);
        danceController.StartDance();
    }
}
