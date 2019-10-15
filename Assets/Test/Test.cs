using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour {
    public List<Sprite> sprites;
    public VirtualListView virtualListView;
	// Use this for initialization
	void Start () {
        virtualListView.onShowNewItem.AddListener(showNewCard);
        virtualListView.onHideItem.AddListener(hideCard);
        virtualListView.SetItemCount(11);
    }
    
    void showNewCard(int index, GameObject item)
    {
        int spriteIndex = index % sprites.Count;
        item.GetComponent<Image>().sprite = sprites[spriteIndex];
    }

    void hideCard(int index, GameObject item)
    {
        item.GetComponent<Image>().sprite = null;
    }
}
