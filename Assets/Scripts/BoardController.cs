using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class BoardController : MonoBehaviourPunCallbacks, IPunObservable
{
    private List<Material> slides = new List<Material>();
    private int current = 0;

    void Start()
    {
        // Initializing with an empty or default slide, if needed
        if (slides.Count > 0)
        {
            GetComponent<Renderer>().material = slides[0];
        }
    }

    // Method to load slides for a selected lecture folder
    public void LoadSlidesForLecture(string lectureFolderName)
    {
        // Clear existing slides
        slides.Clear();
        current = 0;

        // Set the images path based on selected lecture
        string imagesPath = Path.Combine("Images", lectureFolderName);  // Use Resources path format
        Object[] textures = Resources.LoadAll(imagesPath, typeof(Texture2D));

        Debug.Log("Loading slides from folder: " + imagesPath);
        Debug.Log("Found " + textures.Length + " slides");

        foreach (Object tex in textures)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = (Texture2D)tex;
            slides.Add(mat);
        }

        // Update the board with the first slide if available
        if (slides.Count > 0)
        {
            GetComponent<Renderer>().material = slides[0];
            Debug.Log("Loaded " + slides.Count + " slides from " + imagesPath);
        }
        else
        {
            Debug.LogError("No slides found in " + imagesPath);
        }
    }

    void Update()
    {
        if (PhotonNetwork.LocalPlayer.UserId != Presenter.Instance.presenterID) return;

        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            photonView.RPC("ChangeSlideRpc", RpcTarget.All, +1);
        }

        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            photonView.RPC("ChangeSlideRpc", RpcTarget.All, -1);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        photonView.RPC("ChangeSlideRpc", RpcTarget.All, 0);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(current);
            photonView.RPC("ChangeSlideRpc", RpcTarget.All, 0);
        }
        else
        {
            current = (int)stream.ReceiveNext();
            photonView.RPC("ChangeSlideRpc", RpcTarget.All, 0);
        }
    }

    [PunRPC]
    public void ChangeSlideRpc(int value)
    {
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
