using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RegressionGames.RGBotConfigs;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwipeAction : RGAction
{
    private Tile targetTile1 = null;
    private Tile targetTile2 = null;

    public override string GetActionName()
    {
        return "Swipe";
    }

    public override void StartAction(Dictionary<string, object> input)
    {

        // Grab the tiles to swap
        int x1 = int.Parse(input["x1"].ToString());
        int y1 = int.Parse(input["y1"].ToString());
        int x2 = int.Parse(input["x2"].ToString());
        int y2 = int.Parse(input["y2"].ToString());

        // Grab the tiles
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
