using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class testRandom : MonoBehaviour
{
    // Chances Setting
    private float[] chances = { 0.28f, 0.5f, 0.6f };
    private int[] match_list = { 0, 0, 0 };
    private float random_value;

    public GameObject[] events;

    int index_element;
    // Start is called before the first frame update
    private void OnTriggerEnter()
    {
        float more_max = Mathf.Max(chances.ToArray());
        float less_min = Mathf.Min(chances.ToArray());
        int cnt_from_max = 1;
        int cnt_from_min = 0;
        int len_chances = chances.Length;

        for (int i = 0; i < chances.Length * 2; ++i)
        {
            random_value = Random.value;
            if (random_value >= more_max)
            {
                match_list[len_chances - cnt_from_max] += 1;
                cnt_from_max++;
                if (cnt_from_max == len_chances) cnt_from_max = 1;
            }
            if (random_value <= less_min)
            {
                match_list[cnt_from_min] += 1;
                cnt_from_min++;
                if (cnt_from_min == len_chances) cnt_from_min = 0;
            }
            else
            {
                for (int recalculation = 0; recalculation < chances.Length; ++recalculation)
                {
                    if (random_value > chances[recalculation])
                    {
                        match_list[recalculation] += 1;
                        match_list[recalculation + 1] += 1;
                        break;
                    }
                }
            }
        }

        int max_number_from_list = Mathf.Max(match_list.ToArray());
        int count_max_number = match_list.Count(x => x == max_number_from_list);
        if (count_max_number == 1)
        {
            index_element = System.Array.IndexOf(match_list, max_number_from_list);
        }
        else index_element = Random.Range(0, match_list.Length - 1);

        Instantiate(events[index_element], events[index_element].transform.position, Quaternion.identity);
    }

    private void OnTriggerExit()
    {

    }
}
