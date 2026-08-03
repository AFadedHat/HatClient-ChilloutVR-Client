using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using UnityEngine;

namespace BTKUILib;

public class UIPlayerObject
{
    internal ABI_RC.Systems.UI.UILib.UIPlayerObject InternalPlayerObject;

    public UIPlayerObject(ABI_RC.Systems.UI.UILib.UIPlayerObject internalPlayerObject)
    {
        InternalPlayerObject = internalPlayerObject;
    }

    public CVRPlayerEntity CVRPlayer => InternalPlayerObject.CVRPlayer;

    public GameObject AvatarObject => InternalPlayerObject.AvatarObject;

    public string Uuid => InternalPlayerObject.Uuid;

    public string Username => InternalPlayerObject.Username;

    public Animator AvatarAnimator => InternalPlayerObject.AvatarAnimator;

    public GameObject PlayerGameObject => InternalPlayerObject.PlayerGameObject;

    public string AvatarID => InternalPlayerObject.AvatarID;

    public string PlayerIconURL => InternalPlayerObject.PlayerIconURL;

    public bool IsLocalUser => InternalPlayerObject.IsLocalUser;


    public override string ToString()
    {
        return $"UIPlayerObject - [Uuid: {Uuid}, Username: {Username}]";
    }


    public override bool Equals(object obj)
    {
        if(obj is UIPlayerObject playerObject)
            return Uuid == playerObject.Uuid;
        return false;
    }
}