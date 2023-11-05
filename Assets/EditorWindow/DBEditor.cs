using System;
using System.IO;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[Serializable]
public class DBEditor : EditorWindow
{
    private DBConnectionData _dbEditor;

    [MenuItem("Tools/DB Connection")]
    public static void ShowVoxelEditor()
    {
        // This method is called when the user selects the menu item in the Editor
        var wnd = GetWindow<DBEditor>();
        wnd.titleContent = new GUIContent("DB Connection");
    }

    private void OnEnable()
    {
        hideFlags = HideFlags.HideAndDontSave;
        if (AssetDatabase.LoadAssetAtPath("Assets/Resources/ConnectionData.asset", typeof(DBConnectionData)) == null)
        {
            _dbEditor = CreateInstance<DBConnectionData>();
        }
        else
        {
            _dbEditor = (DBConnectionData)AssetDatabase.LoadAssetAtPath("Assets/Resources/ConnectionData.asset", typeof(DBConnectionData));
        }
    }

    public string GetConnectionString()
    {
        if (_dbEditor.Host == "" || _dbEditor.Username == "" || _dbEditor.Password == "" ||
            _dbEditor.Database == "")
        {
            throw new InvalidDataException("Database connection field are not set up");
        }

        return $"Host={_dbEditor.Host}; Username={_dbEditor.Username}; Password={_dbEditor.Password}; Database={_dbEditor.Database}";
    }

    private void OnGUI()
    {
        GUILayout.Label("Database connection");
        _dbEditor.Host = EditorGUILayout.TextField("Host", _dbEditor.Host);
        _dbEditor.Username = EditorGUILayout.TextField("Username", _dbEditor.Username);
        _dbEditor.Password = EditorGUILayout.TextField("Password", _dbEditor.Password);
        _dbEditor.Database = EditorGUILayout.TextField("Database", _dbEditor.Database);
    }
    
    void OnDestroy()
    {
        if (AssetDatabase.LoadAssetAtPath("Assets/Resources/ConnectionData.asset", typeof(DBConnectionData)) == null)
        {
            AssetDatabase.CreateAsset(_dbEditor, "Assets/Resources/ConnectionData.asset");
        }
        else
        {
            EditorUtility.SetDirty(_dbEditor);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif