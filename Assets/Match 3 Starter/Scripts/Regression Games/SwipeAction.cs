using System;
using System.Collections;
using RegressionGames;
using UnityEngine;

public class SwipeAction : MonoBehaviour
{
    [RGAction("Swipe")]
    public void SwipeTilePositions(int x1, int y1, int x2, int y2)
    {
        // Grab the tiles to swap
        GameObject tile1 = BoardManager.instance.GetTile(x1, y1);
        GameObject tile2 = BoardManager.instance.GetTile(x2, y2);
        
        IEnumerator SwapTiles()
        {
            tile1.GetComponent<Tile>().OnMouseDown();
            var startTime = DateTime.Now;
            yield return new WaitUntil(() => (DateTime.Now - startTime).TotalSeconds > 0.25);
            tile2.GetComponent<Tile>().OnMouseDown();
        }
        StartCoroutine(SwapTiles());

    }
    
}
