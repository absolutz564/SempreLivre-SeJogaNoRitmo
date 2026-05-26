#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Cria o AnimationClip e AnimatorController para o popup de rating (bounce de escala)
/// e conecta tudo no DanceHUD da cena aberta.
/// Use: JustDance > 4. Criar Animação do Rating Popup
/// </summary>
public static class CreateRatingAnimatorTool
{
    const string k_Dir      = "Assets/JustDance/Animations";
    const string k_ClipPath = k_Dir + "/RatingShow.anim";
    const string k_CtrlPath = k_Dir + "/RatingPopupAnimator.controller";

    [MenuItem("JustDance/4. Criar Animação do Rating Popup", priority = 4)]
    public static void CreateAndWire()
    {
        EnsureFolder();

        var clip = GetOrCreateClip();
        var ctrl = GetOrCreateController(clip);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!WireToScene(ctrl))
            return;

        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("JustDance ✓",
            "Animação criada e conectada!\n\n" +
            "Assets criados em Assets/JustDance/Animations/\n" +
            "• RatingShow.anim  (bounce 0 → 1.3 → 1.0 em 0.35s)\n" +
            "• RatingPopupAnimator.controller\n\n" +
            "O campo ratingAnimator dos PlayerHUDs foi preenchido automaticamente.",
            "OK");
    }

    // ── Criação de assets ─────────────────────────────────────────────────────

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(k_Dir))
            AssetDatabase.CreateFolder("Assets/JustDance", "Animations");
    }

    static AnimationClip GetOrCreateClip()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(k_ClipPath);
        if (existing != null) return existing;

        var clip = new AnimationClip { name = "RatingShow" };

        // Bounce de escala: parte de 0, ultrapassa 1.3 e pousa em 1.0
        var curve = new AnimationCurve(
            new Keyframe(0.00f, 0.0f,  0f,  14f),   // início rápido
            new Keyframe(0.18f, 1.3f,  0f,   0f),   // overshoot
            new Keyframe(0.35f, 1.0f, -3f,   0f)    // pouso suave
        );

        // RectTransform herda de Transform — m_LocalScale está em Transform
        clip.SetCurve("", typeof(RectTransform), "m_LocalScale.x", curve);
        clip.SetCurve("", typeof(RectTransform), "m_LocalScale.y", curve);
        clip.SetCurve("", typeof(RectTransform), "m_LocalScale.z",
            AnimationCurve.Constant(0f, 0.35f, 1f));

        AssetDatabase.CreateAsset(clip, k_ClipPath);
        return clip;
    }

    static AnimatorController GetOrCreateController(AnimationClip clip)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(k_CtrlPath);
        if (existing != null) return existing;

        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(k_CtrlPath);
        ctrl.AddParameter("Show", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        // Estado neutro (escala normal, sem motion)
        var idle = sm.AddState("Idle");
        sm.defaultState = idle;

        // Estado de animação
        var show = sm.AddState("Show");
        show.motion = clip;

        // Any State → Show quando trigger "Show" for disparado
        var toShow = sm.AddAnyStateTransition(show);
        toShow.AddCondition(AnimatorConditionMode.If, 0f, "Show");
        toShow.duration            = 0f;
        toShow.hasExitTime         = false;
        toShow.canTransitionToSelf = false;

        // Show → Idle ao fim da animação (deixa o objeto estável em escala 1)
        var toIdle = show.AddTransition(idle);
        toIdle.hasExitTime = true;
        toIdle.exitTime    = 1f;
        toIdle.duration    = 0f;

        return ctrl;
    }

    // ── Wiring na cena ────────────────────────────────────────────────────────

    static bool WireToScene(AnimatorController ctrl)
    {
        var playerHUDs = Object.FindObjectsOfType<PlayerHUD>();
        if (playerHUDs.Length == 0)
        {
            EditorUtility.DisplayDialog("JustDance — Erro",
                "Nenhum componente PlayerHUD encontrado na cena.\n\n" +
                "Execute primeiro:\n" +
                "JustDance > 6. Configurar Multi-Jogador", "OK");
            return false;
        }

        int wired = 0;
        foreach (var phud in playerHUDs)
        {
            if (phud.ratingImage == null) continue;
            var anim = phud.ratingImage.gameObject.GetComponent<Animator>()
                    ?? phud.ratingImage.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            phud.ratingAnimator = anim;
            EditorUtility.SetDirty(phud);
            wired++;
        }

        if (wired == 0)
        {
            EditorUtility.DisplayDialog("JustDance — Aviso",
                "PlayerHUDs encontrados mas nenhum tem 'ratingText' atribuído.\n\n" +
                "Execute: JustDance > 6. Configurar Multi-Jogador", "OK");
            return false;
        }

        return true;
    }
}
#endif
