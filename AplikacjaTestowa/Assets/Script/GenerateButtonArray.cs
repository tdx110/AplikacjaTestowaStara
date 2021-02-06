using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#region Informacje ode mnie o programie
/// <summary>
///W tej wersji brakuje kilku zabezpieczeń.
///np.: Jesli będzie generowana większa ilość przycisków niż jest w stanie pomieścić na ekranie.
///Później (na sam koniec) podczas pisanie przy tych wszystkich zmiennych doszedłem do wniosku
///że mogłem te wszystkie informacje zapisać pod postacią jednej większej klasy z danymi.
///Jestem niemal pewien że skróciło by to czas potrzebny na działania i obliczenia
///Niestety jako że w piątek zacząłem pisać program to miałem mało czasu na jego napisanie.
///Kilka innych aplikacji także piszę, poprawiam własną grę i się uczę więc póki nie mam
///pracy to muszę to jakoś pogodzić.
///Do Patchfindingu nie korzystałem z żadnych gotowych rozwiązań, ponieważ są płatne,
///albo nie spełniają moich oczekiwań
///Jako że jest to mały program to wszystko napisałem w jednym skrypcie.
///Normalnie to bym to rozłożył na klasy, ponieważ nie wiadomo gdzie mogła by
///się przydać jakaś funkcja
/// </summary>
#endregion

public class GenerateButtonArray : MonoBehaviour
{
    #region Zmienne prywatne
    [SerializeField]
    private ObjectClass objectClass;
    [SerializeField]
    private DataInputClass dataInputClass;
    //Całkowita wysokość przycisków i ich położenie od góry ekranu
    private float allDistance;
    //Tablica przycisków
    private Button[,] buttonArray;
    //Tablica o takich samych wymiatach jak z przycisków
    //do przechowywania wartości każdego przycisku
    private patchFinding[,] patchFindings;
    //Tablica przechowująca informacje o najkrótszej
    private bool[,] track;
    //Tabela przechowująca informacje o tym gdzie już byłpodczas wyznaczania drogi
    private bool[,] whereIWas;
    //Wymiary ekranu pobrane na początku aby dostosować rozdzielczość i wymiary przycisków
    private float screenSizeX, screenSizeY;
    //Czy ustawiać punkt początkowy czy końcowy
    private bool StartPoint = true;
    //Czy można ustawiać przycisk Start i Koniec
    private bool EnableClick = true;
    //Za co odpowiadają konkretne kolory
    private Color colorStart = Color.green;
    private Color colorEnd = Color.yellow;
    private Color colorDisable = Color.black;
    private Color colorTrack = Color.blue;
    //Wątki dodatkowe
    private List<Action> listAction;
    //współrzędne punktów początkowego i końcowego
    private int startX, startY;
    private int endX, endY;
    //współrzędne przycisku do pomalowania
    private int paintX, paintY;
    private bool painting;
    //Maksymalna wartość Summary. Jeśli po niej nie znajdzie drogi
    //to komunikat że nie ma dojścia
    private int maxSummary;
    //Czy zakończyło odpowiednie działania w wątkach dodatkowych
    private bool finishDistanceToStart, finishDistanceToEnd;
    #endregion
    void Start()
    {
        #region Ustawianie początkowych parametrów
        finishDistanceToEnd = false;
        finishDistanceToStart = false;
        maxSummary = 0;
        listAction = new List<Action>(0);
        screenSizeX = objectClass.cameraMainCamera.pixelWidth;
        screenSizeY = objectClass.cameraMainCamera.pixelHeight;
        float posY = Math.Abs(objectClass.generateButton.GetComponent<RectTransform>().anchoredPosition.y);
        float height = objectClass.generateButton.GetComponent<RectTransform>().sizeDelta.y;
        allDistance = posY + height;
        painting = false;
        #endregion
    }
    private void Update()
    {

        #region Na późniejsze potrzeby jeśli będzie potrzeba
        //Lista funkcji jakie zostaną wykonane

        while (listAction.Count > 0)
        {
            Action action = listAction[0];
            action();
            listAction.RemoveAt(0);
        }
        //Liczy w osobnych wątkach odległość od Start i End
        if (finishDistanceToEnd && finishDistanceToStart)
        {
            StartChildThread(summaryDistance);
            finishDistanceToStart = false;
            finishDistanceToEnd = false;
        }
        #endregion

    }
    public void GenerateArray()
    {
        int sizeX;
        int sizeY;

        if (objectClass.InputX.text != "" && objectClass.InputY.text != "")
        {
            sizeX = int.Parse(objectClass.InputX.text);
            sizeY = int.Parse(objectClass.InputY.text);
        }
        else
        {
            sizeX = 0;
            sizeY = 0;
        }
        if (sizeX > 1 && sizeY > 1)
        {
            #region Czyszczenie wcześniejszej tablicy
            //Jeśli wcześniej była tablica to niszczy wszystkie obiekty w niej zawarte
            if (buttonArray != null)
            {
                for (int y = 0; y < buttonArray.GetLength(1); y++)
                {
                    for (int x = 0; x < buttonArray.GetLength(0); x++)
                    {
                        Destroy(buttonArray[x, y].gameObject);
                    }
                }
            }
            #endregion
            #region Ustawianie wymiaru tablicy i przycisków
            //Ustawianie rozmiru tablicy
            buttonArray = new Button[sizeX, sizeY];
            patchFindings = new patchFinding[sizeX, sizeY];
            //Liczenie wymiaru przycisku
            float ButtonXSize = (screenSizeX - 2 * dataInputClass.distanceLeftRight - (sizeX - 1) *
                dataInputClass.distanceButtomToButtom) / sizeX;
            float ButtonYSize = (screenSizeY - dataInputClass.distanceButtom - dataInputClass.distanceFromButton - allDistance
                - (sizeY - 1) * dataInputClass.distanceButtomToButtom) / sizeY;
            #endregion
            #region Tworzenie tablicy przycisków
            //Wyłącza na czas tworzenia przytcisków obiekt rodzica
            //Przyspieszy to tworzenie przycisków
            //(według Diagnostyki z 797ms na 730ms dla poka 50x50)
            objectClass.arrayParent.SetActive(false);
            //Licznik
            int count = 1;
            //pozycje przycisku na scenie względem lewego górnego punktu
            float vectX, vectY;
            //Resetowanie ustawień aby można było znowu zaznaczać przyciski
            StartPoint = true;
            EnableClick = true;
            //Tworzenie tablicy
            for (int y = 0; y < sizeY; y++)
            {
                if (sizeY == 1) vectY = -1 * allDistance - (y * ButtonYSize) - dataInputClass.distanceFromButton;
                else vectY = -1 * allDistance - (y * ButtonYSize) - dataInputClass.distanceFromButton -
                        (y * dataInputClass.distanceButtomToButtom);
                for (int x = 0; x < sizeX; x++)
                {
                    int posX, posY;
                    posX = x;
                    posY = y;
                    if (sizeX == 1) vectX = dataInputClass.distanceLeftRight + (x * ButtonXSize);
                    else vectX = dataInputClass.distanceLeftRight + (x * ButtonXSize) + (x * dataInputClass.distanceButtomToButtom);
                    buttonArray[x, y] = Instantiate(objectClass.buttonConstructor);
                    buttonArray[x, y].transform.SetParent(objectClass.arrayParent.transform);
                    buttonArray[x, y].name = "Button [" + x + "," + y + "]";
                    buttonArray[x, y].transform.Find("Text").GetComponent<Text>().text = "[" + (x + 1) + "," + (y + 1) + "]";
                    buttonArray[x, y].transform.Find("Text").GetComponent<Text>().enabled = false;
                    //Ustawianie obsługi naciśnięcie przycisku
                    buttonArray[x, y].onClick.AddListener(delegate { sartEndPointButton(ref buttonArray[posX, posY]); });
                    buttonArray[x, y].GetComponent<RectTransform>().anchoredPosition = new Vector3(vectX, vectY);
                    buttonArray[x, y].GetComponent<RectTransform>().sizeDelta = new Vector3(ButtonXSize, ButtonYSize);
                    //Co który przycisk ma być wyłączony
                    if (count % 13 == 0)
                    {
                        count = 1;
                        disableButton(ref buttonArray[x, y]);
                    }
                    else
                    {
                        count = count + 1;
                        enableButton(ref buttonArray[x, y]);
                    }
                }
            }
            #endregion
            objectClass.arrayParent.SetActive(true);
            Debug.Log("Zakończono tworzenie przycisków");
        }
        else
        {
            Debug.LogError("Co najmniej wymiar wynosi 0");
        }
    }
    public void ExitApplication()
    {
        Application.Quit();
    }
    #region Zmiana parametrów przycisków
    private void disableButton(ref Button button)
    {
        ColorBlock colorBlock;
        colorBlock = button.colors;
        colorBlock.disabledColor = colorDisable;

        button.colors = colorBlock;

        button.interactable = false;

    }
    private void enableButton(ref Button button)
    {
        button.interactable = true;
        button.colors = ColorBlock.defaultColorBlock;
    }
    private void sartEndPointButton(ref Button button)
    {
        if (EnableClick)
        {
            if (StartPoint)
            {
                ColorBlock colorBlock;
                colorBlock = button.colors;
                colorBlock.disabledColor = colorStart;
                button.colors = colorBlock;

                StartPoint = false;
                button.interactable = false;
                button.transform.Find("Text").GetComponent<Text>().enabled = true;
            }
            else
            {
                ColorBlock colorBlock;
                colorBlock = button.colors;
                colorBlock.disabledColor = colorEnd;
                button.colors = colorBlock;

                button.interactable = false;
                EnableClick = false;
                button.transform.Find("Text").GetComponent<Text>().enabled = true;
                Debug.Log("Uruchamia wątek dodatkowy.");
                StartChildThread(checkButton);
            }
        }
    }
    private void paintButton()
    {
        ColorBlock colorBlock = buttonArray[paintX, paintY].colors;
        colorBlock.disabledColor = colorTrack;
        buttonArray[paintX, paintY].colors = colorBlock;
        buttonArray[paintX, paintY].interactable = false;
        painting = false;
    }
    #endregion
    #region Funkcje dla wielu wątków
    /// <summary>
    /// Funkcja tworzy nowy wątek, który wykona nstępującą funkcję
    /// </summary>
    /// <param name="action">funkcja jaką wykona dodatkowy wątek</param>
    public void StartChildThread(Action _action)
    {
        Thread thread = new Thread(new ThreadStart(_action));
        thread.Start();
    }
    /// <summary>
    /// Metoda do wykonania w głównym wątku
    /// </summary>
    /// <param name="action"></param>
    public void StartMainThread(Action action)
    {
        listAction.Add(action);
    }

    //Funkcja generująca najkrótszą drogę
    //Funkcja w nowym watku
    private void GeneratePathFinding()
    {
        int min;
        int minX, minY;
        minX = minY = 0;
        whereIWas = new bool[patchFindings.GetLength(0), patchFindings.GetLength(1)];
        for (int i = 0; i < 10; i++)
        {
            min = maxSummary;
            for (int y = 0; y < patchFindings.GetLength(1); y++)
            {
                for (int x = 0; x < patchFindings.GetLength(0); x++)
                {
                    if (patchFindings[x, y].Track != typeButtonEnum.disable && patchFindings[x, y].Track != typeButtonEnum.start &&
                        patchFindings[x, y].Track != typeButtonEnum.end && whereIWas[x, y] != true)
                    {
                        bool pai = false;
                        //rozpatrywanie 
                        if (x==0)
                        {
                            if (y == 0)
                            {
                                if (true)
                                {

                                }
                            }
                            else
                            {

                            }
                        }
                        else
                        {
                            if (y ==0)
                            {

                            }
                        }

                        if (pai)
                        {

                        }
                    }
                        if (patchFindings[x, y].Track != typeButtonEnum.disable && patchFindings[x, y].Track != typeButtonEnum.start &&
                        patchFindings[x, y].Track != typeButtonEnum.end && whereIWas[x, y] != true)
                    {
                        
                        if (min > patchFindings[x, y].Summary)
                        {
                            minX = x;
                            minY = y;
                            min = patchFindings[x, y].Summary;
                        }
                    }
                }
            }
            whereIWas[minX, minY] = true;
            paintX = minX;
            paintY = minY;
            painting = true;
            StartMainThread(paintButton);
            while (painting)
            {

            }
        }

        Debug.Log("Zakończono znajdywanie najkrótszej drogi.");
    }
    private void checkButton()
    {
        for (int y = 0; y < patchFindings.GetLength(1); y++)
        {
            for (int x = 0; x < patchFindings.GetLength(0); x++)
            {
                if (buttonArray[x, y].interactable)
                {
                    patchFindings[x, y] = new patchFinding(typeButtonEnum.track);
                }
                else
                {
                    if (buttonArray[x, y].colors.disabledColor == colorStart)
                    {
                        startX = x;
                        startY = y;
                        patchFindings[x, y] = new patchFinding(typeButtonEnum.start);
                    }
                    else if (buttonArray[x, y].colors.disabledColor == colorEnd)
                    {
                        endX = x;
                        patchFindings[x, y] = new patchFinding(typeButtonEnum.end);
                        endY = y;
                    }
                    else
                    {
                        patchFindings[x, y] = new patchFinding(typeButtonEnum.disable);
                    }
                }

            }
        }
        StartChildThread(distanceToStart);
        StartChildThread(distanceToEnd);
        Debug.Log("Zakończono sprawdzanie rodzaji przycisków");
    }
    private void distanceToStart()
    {
        for (int y = 0; y < patchFindings.GetLength(1); y++)
        {
            for (int x = 0; x < patchFindings.GetLength(0); x++)
            {
                patchFindings[x, y].DistanceToStart = (int)Math.Round((
                    Math.Sqrt(Math.Abs(startX - x) + Math.Abs(startY - y))) * 10, 0);
            }
        }
        finishDistanceToStart = true;
        Debug.Log("Zakończono obliczanie odległości od Przycisku Start.");
    }
    private void distanceToEnd()
    {
        for (int y = 0; y < patchFindings.GetLength(1); y++)
        {
            for (int x = 0; x < patchFindings.GetLength(0); x++)
            {
                patchFindings[x, y].DistanceToEnd = (int)Math.Round((
                    Math.Sqrt(Math.Abs(endX - x) + Math.Abs(endY - y))) * 10, 0);
            }
        }
        finishDistanceToEnd = true;
        Debug.Log("Zakończono obliczanie odległości od Przycisku Start.");
    }
    private void summaryDistance()
    {
        for (int y = 0; y < patchFindings.GetLength(1); y++)
        {
            for (int x = 0; x < patchFindings.GetLength(0); x++)
            {
                int sum = patchFindings[x, y].DistanceToStart + patchFindings[x, y].DistanceToEnd;
                patchFindings[x, y].Summary = sum;
                if (sum > maxSummary)
                {
                    maxSummary = sum;
                }
            }
        }
        StartChildThread(GeneratePathFinding);
        Debug.Log("Zakończono sumowanie odległości.");
    }
    #endregion

    #region Dane wejściowe
    [Serializable]
    public class ObjectClass
    {
        [Header("Kamera")]
        public Camera cameraMainCamera;
        [Header("Przycisk do konstruktora.")]
        public Button buttonConstructor;
        [Header("Przycisk Generate")]
        public GameObject generateButton;
        [Header("Pole InputX")]
        public TMPro.TMP_InputField InputX;
        [Header("Pole InputY")]
        public TMPro.TMP_InputField InputY;
        [Header("Rodzic siatki przycisków")]
        public GameObject arrayParent;
    }
    [Serializable]
    public class DataInputClass
    {
        [Header("Odstęp siatki od przycisków")]
        public float distanceFromButton;
        [Header("Dystans od boków")]
        public float distanceLeftRight;
        [Header("Dystans od dołu")]
        public float distanceButtom;
        [Header("Dystans miedzy przyciskami")]
        public float distanceButtomToButtom;
    }
    #endregion
    #region Klasy i Enum-y
    private class patchFinding
    {
        int distanceToStart;
        int distanceToEnd;
        int summary;
        typeButtonEnum track;
        public patchFinding(typeButtonEnum _track)
        {
            track = _track;
        }
        public int DistanceToStart
        {
            get { return distanceToStart; }
            set { distanceToStart = value; }
        }
        public int DistanceToEnd
        {
            get { return distanceToEnd; }
            set { distanceToEnd = value; }
        }
        public int Summary
        {
            get { return summary; }
            set { summary = value; }
        }
        public typeButtonEnum Track
        {
            get { return track; }
            set { track = value; }
        }
    }
    private enum typeButtonEnum
    {
        track,
        disable,
        start,
        end,
    }
    #endregion
}
