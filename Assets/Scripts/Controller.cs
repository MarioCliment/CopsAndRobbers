﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        // Matriz de adyacencia
        // Porque es 64x64 (i,j)? Pues porque i representa la casilla y j representa las casillas adjacentes a esa casilla
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //TODO: Inicializar matriz a 0's
        for (int i=0; i<Constants.NumTiles; i++)
        {
            for(int j=0; j<Constants.NumTiles-1; j++)
            {
                // simplemente, inicializamos todo a 0
                matriu[i, j]=0;
            }
        }

        //TODO: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                // i>7 porque cualquier casilla (i) que no sea mayor de 7 es porque está en la fila de abajo del todo, lo 
                // que quiere decir que no hay más casillas por debajo
                // abajo
                if (i>7)  matriu[i, i - 8] = 1;
                // i<56 es un caso similar, si la casilla no es menor que 56 es porque esta en la fila de arriba del todo!
                // arriba
                if (i<56) matriu[i, i + 8] = 1;
                // Todas las casillas de la izquierda del todo son múltiplos de 8, con lo que no tienen nada a la izquierda si el resto es 0
                // izquierda
                if (i%8!=0) matriu[i, i - 1] = 1;
                // lo mismo con las de la derecha, pero al dividir entre 8, las que tengan resto de 7, están a la derecha (23-8 = 15 - 8 = 7) 
                // derecha
                if (i%8!=7) matriu[i, i + 1] = 1;
            }
        }

        //TODO: Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for (int i = 0; i<Constants.NumTiles; i++)
        {
            for (int j = 0; j<Constants.NumTiles; j++)
            {
                // ¡Joe Biden!
                // Aqui añadimos la j a las casillas! Que si te fijas arriba, en los i - 8, i + 8, etc, esos son las j!
                // Y por supuesto, solo añadimos las que son 1, es decir, las que son adyacentes!
                if (matriu[i, j] == 1) tiles[i].adjacency.Add(j);
            }
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        //TODO: Cambia el código de abajo para hacer lo siguiente
        //- Elegimos una casilla aleatoria (ya no es aleatoria! ahora es la mas lejana!) entre las seleccionables que puede ir el caco
        Tile casillaLejosPolis = GetFurthestTileFromCops();
        int casillaLejosPolisNumero = casillaLejosPolis.numTile;
        //- Movemos al caco a esa casilla
        robber.GetComponent<RobberMove>().MoveToTile(casillaLejosPolis);
        //- Actualizamos la variable currentTile del caco a la nueva casilla
        robber.GetComponent<RobberMove>().currentTile = casillaLejosPolisNumero;

    }

    public Tile GetFurthestTileFromCops()
    {
        // Tiene que ser null, si no Unity llora que te cagas
        Tile furthest = null;
        float distanceToCops;
        float auxDistance = 0;
        //GameObject nearestCop = null;
        foreach (Tile tile in tiles)
        {
            if (tile.selectable)
            {
                distanceToCops = 0;
                Debug.Log("Mirando la casilla " + tile.numTile);
                //Compruebas todos los policias, en el caso de que la casilla anterior sea mayor a la obtenida nuevamente
                //se sustituye automaticamente en el bucle, siendo este un sistema modular ya que puedes tener tantos
                //policias como quieras

                foreach (GameObject cop in cops)
                {
                    distanceToCops = Vector3.Distance(tile.transform.position, cop.transform.position) + distanceToCops;
                }
                Debug.Log("Distancia a los polis " + distanceToCops);
                if (distanceToCops > auxDistance)
                {
                    Debug.Log("Distancia a los polis es mayor " + distanceToCops);
                    auxDistance = distanceToCops;
                    furthest = tile;
                }
            }
        }
        Debug.Log("Casilla mas lejana" + furthest.numTile);
        return furthest;
    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
                 
        int indexcurrentTile;        

        if (cop==true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        //TODO: Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        //Tendrás que cambiar este código por el BFS
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            // inicializamos todas las casillas como false
            tiles[i].selectable = false;
        }

        // Y aqui simplemente asignamos a las casillas adyacentes de la posicion actual (indexcurrentTile) el atributo de seleccionables,
        // y también a las adyacentes de las adyacentes
        foreach (int casillaAdyacente in tiles[indexcurrentTile].adjacency)
        {
            tiles[casillaAdyacente].selectable = true;

            foreach (int casillaAdyacente2 in tiles[casillaAdyacente].adjacency)
            {
                tiles[casillaAdyacente2].selectable = true;
            }
        }


    }
    
   
    

    

   

       
}
