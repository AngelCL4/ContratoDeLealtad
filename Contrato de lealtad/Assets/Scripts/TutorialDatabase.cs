using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialDatabase", menuName = "Juego/TutorialDatabase")]
public class TutorialDatabase : ScriptableObject
{
    public List<TutorialItem> tutoriales;
}

[System.Serializable]
public class TutorialItem
{
    public string titulo;
    
    [TextArea(3, 10)]
    public string descripcion;

    public bool desbloqueadoDesdeInicio = true;

    [Tooltip("Identificador interno para desbloqueo dinámico")]
    public string id; // Ej: "cap1_intro", "uso_hacha", etc.
}