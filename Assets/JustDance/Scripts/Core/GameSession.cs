/// Dados globais da sessão corrente. Acessível de qualquer script sem MonoBehaviour.
public static class GameSession
{
    public static int               PlayerCount           { get; set; } = 1;
    public static DanceChoreography SelectedChoreography  { get; set; }
}
