using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    public int SceneIndex;

    public void LoadSceneByIndex() {
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneIndex);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
