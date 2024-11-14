using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarList : MonoBehaviour {

	float size = 0.2f;
	float delta = 0.222f;
	List<Avatar2D> avatars = new List<Avatar2D>();

	public void addToList(Avatar2D avatar) {

		//set trans
		avatar.transform.parent = transform;
		avatar.transform.localScale = new Vector3(size, size, size);
		avatar.transform.localPosition = new Vector3(delta * avatars.Count, 0,0);
		avatar.transform.localRotation = Quaternion.identity;
		avatar.parent = this;

		//add to list
		avatars.Add(avatar);

	}

	public void removeFromList(Avatar2D avatar) {

		//remove from list
		avatars.Remove(avatar);
		avatar.transform.parent = null;
		avatar.transform.localPosition = Vector3.zero;
		avatar.parent = null;

		//re-add remaining avars to set the pos properlly
		List<Avatar2D> tempAvatars = new List<Avatar2D>();
		for(int x = 0; x < avatars.Count; x++) {
			tempAvatars.Add(avatars[x]);
		}
		avatars.Clear();
		for (int x = 0; x < tempAvatars.Count; x++) {
			addToList(tempAvatars[x]);
		}
		
	}

}
