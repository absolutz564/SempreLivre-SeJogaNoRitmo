using UnityEngine;
using UnityEngine.UI;

public class RawImageRotator : MonoBehaviour
{
    [Header("Imagens a rotacionar")]
    public RawImage[] targets;

    [Header("Configuração")]
    public KeyCode rotateKey = KeyCode.R;
    public float angleStep = 90f;

    private const string PrefKey = "RawImageRotator_Steps";

    public int CurrentSteps { get; private set; }

    void Start()
    {
        CurrentSteps = PlayerPrefs.GetInt(PrefKey, 0);
        Debug.Log($"[RawImageRotator] Aplicando rotação salva: {CurrentSteps * angleStep}° ({CurrentSteps} passo(s))");

        for (int i = 0; i < CurrentSteps; i++)
            RotateAll();
    }

    void Update()
    {
        if (Input.GetKeyDown(rotateKey))
        {
            RotateAll();

            CurrentSteps = (CurrentSteps + 1) % 4;
            PlayerPrefs.SetInt(PrefKey, CurrentSteps);
            PlayerPrefs.Save();

            Debug.Log($"[RawImageRotator] Rotação atualizada: {CurrentSteps * angleStep}° ({CurrentSteps} passo(s)) — salvo no PlayerPrefs");
        }
    }

    public void RotateAll()
    {
        if (targets == null) return;
        foreach (var img in targets)
        {
            if (img == null) continue;
            RotateOne(img.rectTransform);
        }
    }

    void RotateOne(RectTransform rt)
    {
        var euler = rt.localEulerAngles;
        euler.z = (euler.z + angleStep) % 360f;
        rt.localEulerAngles = euler;

        var size = rt.sizeDelta;
        rt.sizeDelta = new Vector2(size.y, size.x);
    }

    // Útil pra resetar a config da câmera durante testes (ex.: chamar de um botão de "Reset")
    public void ResetRotation()
    {
        PlayerPrefs.DeleteKey(PrefKey);
        PlayerPrefs.Save();
        Debug.Log("[RawImageRotator] Rotação resetada no PlayerPrefs — reinicie a cena para aplicar.");
    }
}
