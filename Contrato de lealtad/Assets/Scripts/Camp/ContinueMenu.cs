using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContinueMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Transform levelList; // El contenedor de niveles
    [SerializeField] private GameObject levelPrefab; // Prefab de un nivel
    [SerializeField] private TextMeshProUGUI levelDescription; // Texto de descripción

    private List<UnlockedChapter> unlockedChapters; // Datos desbloqueados
    private List<ChapterStatus> chapterStatus; // Estado de capítulos

    private List<GameObject> instantiatedLevels = new(); // Para guardar los prefabs creados
    private int currentSelectionIndex = 0;
    private int currentPageStartIndex = 0; // Desde qué nivel empieza la página actual

    private const int pageSize = 10; // Cuántos niveles por página

    private bool isActive = false;

    public void OpenContinueMenu()
    {
        isActive = true;
        gameObject.SetActive(true);
        LoadLevels();
        UpdateLevelSelection();
    }

    private void Update()
    {
        if (!isActive) return;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentSelectionIndex < Mathf.Min(pageSize - 1, instantiatedLevels.Count - 1))
            {
                currentSelectionIndex++;
            }
            else if (currentPageStartIndex + pageSize < instantiatedLevels.Count)
            {
                currentPageStartIndex += pageSize;
                currentSelectionIndex = 0;
                LoadLevels();
            }
            UpdateLevelSelection();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentSelectionIndex > 0)
            {
                currentSelectionIndex--;
            }
            else if (currentPageStartIndex > 0)
            {
                currentPageStartIndex -= pageSize;
                currentSelectionIndex = 0;
                LoadLevels();
            }
            UpdateLevelSelection();
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            CloseContinueMenu();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            ConfirmLevel(); // Esto lo implementaremos después
        }
    }

    private void LoadLevels()
    {
        // Limpiar niveles antiguos
        foreach (var level in instantiatedLevels)
        {
            Destroy(level);
        }
        instantiatedLevels.Clear();

        // Cargar de nuevo
        var gameManager = GameManager.Instance;
        unlockedChapters = gameManager.unlockedChapterDataJuego.chapters;
        Debug.Log($"¿GameManager existe?: {gameManager != null}");
        Debug.Log($"¿Data cargada?: {gameManager.unlockedChapterDataJuego != null}");
        if (unlockedChapters != null)
        {
            Debug.Log($"Cantidad de capítulos desbloqueados: {unlockedChapters.Count}");
            foreach (var chapter in unlockedChapters)
            {
                Debug.Log($"Chapter: {chapter.chapterName} - Title: {chapter.chapterTitle}  - Descripcion {chapter.description} - Desbloqueado: {chapter.estaDesbloqueado}");
            }
        }
        else
        {
            Debug.Log("UnlockedChapters es NULL");
        }
        chapterStatus = gameManager.chapterDataJuego.chapters;

        int visibleLevels = 0;
        for (int i = 0; i < unlockedChapters.Count; i++)
        {
            var chapter = unlockedChapters[i];
            var status = chapterStatus.Find(c => c.chapterName == chapter.chapterName);

            if (chapter.estaDesbloqueado && (status == null || !status.completed))
            {
                if (visibleLevels >= currentPageStartIndex && visibleLevels < currentPageStartIndex + pageSize)
                {
                    GameObject newLevel = Instantiate(levelPrefab, levelList);
                    newLevel.GetComponentInChildren<TextMeshProUGUI>().text = chapter.chapterTitle;

                    var levelItem = newLevel.AddComponent<LevelItem>();
                    levelItem.chapterData = chapter;

                    instantiatedLevels.Add(newLevel);
                }
                visibleLevels++;
            }
        }
    }

    private void UpdateLevelSelection()
    {
        for (int i = 0; i < instantiatedLevels.Count; i++)
        {
            var textComponent = instantiatedLevels[i].GetComponentInChildren<TextMeshProUGUI>();
            if (i == currentSelectionIndex)
            {
                textComponent.color = Color.yellow; // Seleccionado
                UpdateDescription(i);
            }
            else
            {
                textComponent.color = Color.white; // No seleccionado
            }
        }
    }

    private void UpdateDescription(int index)
    {
        if (index >= 0 && index < instantiatedLevels.Count)
        {
            var levelItem = instantiatedLevels[index].GetComponent<LevelItem>();
            levelDescription.text = levelItem.chapterData.description;
        }
    }

    private void ConfirmLevel()
    {
        var selectedLevelItem = instantiatedLevels[currentSelectionIndex].GetComponent<LevelItem>();
        if (selectedLevelItem != null)
        {
            string chapterName = selectedLevelItem.chapterData.chapterName;
            GameManager.Instance.currentChapter = chapterName;
            Debug.Log("✅ Capítulo confirmado: " + chapterName);
            SceneLoader.Instance.LoadScene("ChapterScene");
            // Aquí podrías cargar la escena o proceder con el juego
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró LevelItem en el nivel seleccionado.");
        }
    }

    private void CloseContinueMenu()
    {
        isActive = false;
        gameObject.SetActive(false);
        CampManager.Instance.isMenuActive = true; // Volver al menú principal
    }
}
