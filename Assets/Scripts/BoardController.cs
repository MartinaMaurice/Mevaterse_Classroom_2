using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class BoardController : MonoBehaviourPunCallbacks, IPunObservable
{
    private List<Material> slides = new List<Material>();
    private int current = 0;
    public TabletManager tabletManager;

    void Start()
    {
        // Load slides with a default lecture folder if available
        string defaultLectureFolder = "Lecture1"; // Replace with your folder name
        LoadSlidesForLecture(defaultLectureFolder);

        if (slides.Count > 0)
        {
            GetComponent<Renderer>().material = slides[0];
        }
        else
        {
            Debug.LogError("No slides loaded at Start. Check the folder path.");
        }
    }

    // Method to load slides for a selected lecture folder
    public void LoadSlidesForLecture(string lectureFolderName)
    {
        slides.Clear();
        current = 0;

        string imagesPath = Path.Combine("Images", lectureFolderName); // Path relative to Resources
        Object[] textures = Resources.LoadAll(imagesPath, typeof(Texture2D));

        Debug.Log($"Attempting to load slides from: Resources/{imagesPath}");
        Debug.Log($"Number of slides found: {textures.Length}");

        if (textures.Length == 0)
        {
            Debug.LogError($"No slides found in path: Resources/{imagesPath}. Check folder structure and file names.");
            return;
        }

        foreach (Object tex in textures)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = (Texture2D)tex;
            slides.Add(mat);
        }

        if (slides.Count > 0)
        {
            GetComponent<Renderer>().material = slides[0];
            Debug.Log($"Successfully loaded {slides.Count} slides from Resources/{imagesPath}");
        }
        else
        {
            Debug.LogError("Slides list is empty after loading. This should not happen!");
        }
    }


    void Update()
    {
        if (PhotonNetwork.LocalPlayer.UserId != Presenter.Instance.presenterID) return;

        if (Input.GetKeyUp(KeyCode.RightArrow) && slides.Count > 0)
        {
            photonView.RPC("ChangeSlideRpc", RpcTarget.All, +1);
        }

        if (Input.GetKeyUp(KeyCode.LeftArrow) && slides.Count > 0)
        {
            photonView.RPC("ChangeSlideRpc", RpcTarget.All, -1);
        }
    }


    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Ensure slides are loaded before syncing
        if (slides.Count == 0)
        {
            Debug.LogError("Slides are not loaded. Loading default slides.");
            LoadSlidesForLecture("Lecture1");
        }

        photonView.RPC("ChangeSlideRpc", RpcTarget.All, 0);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (slides.Count == 0)
        {
            Debug.LogError("Cannot sync slides because the slides list is empty.");
            return;
        }

        if (stream.IsWriting)
        {
            stream.SendNext(current);
        }
        else
        {
            current = (int)stream.ReceiveNext();
        }
    }


    [PunRPC]
    public void ChangeSlideRpc(int value)
    {
        if (slides.Count == 0)
        {
            Debug.LogError("No slides are loaded. Cannot change slides.");
            return;
        }

        current += value;

        if (current >= slides.Count)
        {
            current = 0;
        }

        if (current < 0)
        {
            current = slides.Count - 1;
        }

        GetComponent<Renderer>().material = slides[current];
    }

}
