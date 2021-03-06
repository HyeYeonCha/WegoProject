﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

public class ClickItem : MonoBehaviour
{
    [SerializeField]
    private GameObject countMessage; // 대량 구매시 개수 선택하는 창

    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private Text rubyCoinUI; // 코인 UI
    
    public int rubyCoin; // 코인 갯수

    public GameObject myContents; // 구매한 내 아이템

    [SerializeField]
    private Text warningText; // 돈이 부족할때 뜨는 메세지
    [SerializeField]
    private Text buttonText; // 대량구매시 누르는 버튼의 text;
    [SerializeField]
    private InputField inputCount; // 대량 구매시 입력하는 아이템의 갯수;

    private int multiplePrice; // 대량 구매시 저장할 아이템의 가격
    private GameObject multipleItem; // 대량 구매시 저장할 게임오브젝트

    private GameObject myItemClones; // 아이템 구매시 생기는 Clone Objects

    Ray ray;
    RaycastHit2D hit;
    Vector3 mousePos;

    bool flag;

    // 화면고정
    void Awake()
    {
        flag = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.SetResolution(1920, 1080, true);
    }

    // Start is called before the first frame update
    async void Start()
    {
        countMessage.SetActive(false);
        inputCount.characterLimit = 3;
        warningText.enabled = false;

        rubyCoin = await AccountManager.Instance.GetTokenBalanceOf();
        rubyCoinUI.text = " : " + rubyCoin;

    }

    // Update is called once per frame
    void Update()
    {
        if(flag)
            PurchaseItem();

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
    }

   // 아이템 다중 일때 슬롯에 개수 보이도록 수정하기 
    private async void PurchaseItem()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (hit.collider != null)
            {
                Item target = hit.collider.gameObject.GetComponent<Item>();

                if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1))
                {
                    countMessage.SetActive(true);
                    multiplePrice = target.ItemInfo.itemCost;
                    multipleItem = target.gameObject;

                    buttonText.text = "'" + target.gameObject.name + "' 아이템 구매?";
                    return;
                }

                if (rubyCoin - target.ItemInfo.itemCost >= 0)
                {
                    flag = false;
                    await AccountManager.Instance.TokenTransferMaster(target.ItemInfo.itemCost);
                    AccountManager.Instance.PurchaseItem(target.ItemInfo.itemName, 1);
                    rubyCoin = await AccountManager.Instance.GetTokenBalanceOf();
                    flag = true;
                    rubyCoinUI.text = " : " + rubyCoin;

                    //AccountManager.Instance.UseItem(hit.collider.gameObject.GetComponent<Item>().ItemInfo.itemName, 1);
                    
                    SlotList.instance.ItemSave(target.gameObject, target.ItemInfo.itemName, 1);

                    if (SlotList.instance.addItem)
                    {
                        myItemClones = Instantiate(target.gameObject, myContents.transform.position, Quaternion.identity);
                        myItemClones.transform.SetParent(myContents.transform, false);
                        myItemClones.GetComponent<BoxCollider2D>().enabled = false;
                        SlotList.instance.itemList.Add(myItemClones);
                        SlotList.instance.itemList.Last().GetComponent<Item>().ItemInfo.itemCount++;
                    }
                } else
                {
                    warningText.enabled = true;
                    StartCoroutine(WarningTextFalse());
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            countMessage.SetActive(false);
        }
    }
    
    // 다중 구매 버튼 클릭시 
    public async void MultipleItemPurchase()
    {
        if (inputCount.text != null && rubyCoin >=0)
        {
            int _count;
            _count = int.Parse(inputCount.text);

            if(_count <1)
            {
                warningText.text = "1보다 큰 값을 입력해주세요";
                warningText.enabled = true;
                StartCoroutine(WarningTextFalse());
                return;
            }

            if(rubyCoin - multiplePrice * _count >= 0)
            {
                flag = false;
                await AccountManager.Instance.TokenTransferMaster(multiplePrice * _count);
                AccountManager.Instance.PurchaseItem(multipleItem.GetComponent<Item>().ItemInfo.itemName, _count);
                rubyCoin = await AccountManager.Instance.GetTokenBalanceOf();
                rubyCoinUI.text = " : " + rubyCoin;
                flag = true;

                SlotList.instance.ItemSave(multipleItem, multipleItem.GetComponent<Item>().ItemInfo.itemName, _count);

                if (SlotList.instance.addItem)
                {
                    myItemClones = Instantiate(multipleItem, myContents.transform.position, Quaternion.identity);
                    myItemClones.transform.SetParent(myContents.transform, false);
                    myItemClones.GetComponent<BoxCollider2D>().enabled = false;
                    SlotList.instance.itemList.Add(myItemClones);
                    SlotList.instance.itemList.Last().GetComponent<Item>().ItemInfo.itemCount += _count;
                }

                ItemCountCheck(myItemClones);

                countMessage.SetActive(false);
            } else
            {
                warningText.enabled = true;
                StartCoroutine(WarningTextFalse());
            }
        } 
    }

    IEnumerator WarningTextFalse()
    {
        yield return new WaitForSeconds(3f);
        warningText.enabled = false;
        warningText.text = "돈이 부족합니다.";
    }

    // 아이템 수량 체크
    public void ItemCountCheck(GameObject item)
    {
        item.GetComponent<Tooltip>().itemCount = item.GetComponent<Item>().ItemInfo.itemCount;

        if (item.GetComponent<Tooltip>().itemCount >= 2)
        {
            item.GetComponent<Tooltip>().countBG.SetActive(true);
            item.GetComponent<Tooltip>().itemCountUI.text = "" + item.GetComponent<Tooltip>().itemCount;
        }
    }
}
