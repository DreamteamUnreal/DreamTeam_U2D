//MainMenuHandler.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HappyHarvest
{
    public class MainMenuHandler : MonoBehaviour
    {
        private UIDocument m_Document;
        private Button m_StartButton;

        private VisualElement m_Blocker;

        private void Start()
        {
            m_Document = GetComponent<UIDocument>();

            // --- ADDED NULL CHECKS FOR ROBUSTNESS (Recommended!) ---
            if (m_Document == null)
            {
                Debug.LogError("MainMenuHandler: UIDocument component not found on this GameObject. Please add a UIDocument.");
                return; // Stop execution if essential component is missing
            }

            if (m_Document.rootVisualElement == null)
            {
                Debug.LogError("MainMenuHandler: UIDocument's rootVisualElement is null. Is the UXML asset assigned and valid?");
                return; // Stop execution if UXML is not loaded
            }

            // FIRST: Query and assign m_Blocker
            m_Blocker = m_Document.rootVisualElement.Q<VisualElement>("Blocker");

            // --- ADDED NULL CHECK FOR Blocker (Recommended!) ---
            if (m_Blocker == null)
            {
                Debug.LogError("MainMenuHandler: VisualElement named 'Blocker' not found in UXML. Check UXML name and type.");
                // You might choose to return here or handle gracefully if Blocker is not critical
            }
            else
            {
                // Register the transition callback for m_Blocker if it exists
                m_Blocker.RegisterCallback<TransitionEndEvent>(evt =>
                {
                    Debug.Log("Blocker transition ended. Loading next scene.");
                    SceneManager.LoadScene(1, LoadSceneMode.Single);
                });
            }


            // SECOND: Query and assign m_StartButton
            m_StartButton = m_Document.rootVisualElement.Q<Button>("StartButton");

            // --- ADDED NULL CHECK FOR StartButton (Recommended!) ---
            if (m_StartButton == null)
            {
                Debug.LogError("MainMenuHandler: Button named 'StartButton' not found in UXML. Check UXML name and type.");
                // You might choose to return here or handle gracefully
            }
            else
            {
                // NOW: Add the click listener to m_StartButton. m_Blocker is now assigned.
                m_StartButton.clicked += () =>
                {
                    Debug.Log("StartButton clicked! Setting blocker opacity.");
                    if (m_Blocker != null) // Redundant check but good for safety if previous check wasn't added
                    {
                        m_Blocker.style.opacity = 1.0f;
                    }
                    else
                    {
                        Debug.LogWarning("MainMenuHandler: Blocker is null when trying to set opacity on StartButton click. Scene will load directly.");
                        // Fallback: If blocker is somehow still null, just load scene directly
                        SceneManager.LoadScene(1, LoadSceneMode.Single);
                    }
                };
            }
        }
    }
}