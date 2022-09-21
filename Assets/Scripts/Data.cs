using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Data : MonoBehaviour
{
    public static void SaveProfile(ProfileData t_profile) {
        try {
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path)) File.Delete(path);

            FileStream file = File.Create(path);

            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(file, t_profile);
            file.Close();

            Debug.Log("Save Profile Success");
        }
        catch {
            Debug.Log("Save Profile Failed");
        }
    }

    public static ProfileData LoadProfile() {
        ProfileData res = new ProfileData();

        try {
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path)) {
                FileStream file = File.Open(path, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                res = (ProfileData)bf.Deserialize(file);

                Debug.Log("Load Profile Success");
            }
        }
        catch {
            Debug.Log("Load Profile Failed");
        }

        return res;
    }
}
