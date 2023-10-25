using System.Collections.Generic;
using RegressionGames.RGBotConfigs;

public class BoardState : RGState
{

    private BoardManager _boardManager;
    
    public void Awake()
    {
        _boardManager = GetComponent<BoardManager>();
    }

    protected override Dictionary<string, object> GetState()
    {
        var state = new Dictionary<string, object>();
        state["tiles"] = _boardManager.GetTileIdentifiers();;
        return state;
    }
}
