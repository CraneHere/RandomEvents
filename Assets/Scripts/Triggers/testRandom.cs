using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class testRandom : MonoBehaviour
{
    
    private float[] chances = { 0.10f, 0.45f, 0.95f };
    private int[] match_list = new int[]{ 0, 0, 0 };
    private float random_value;

    public GameObject[] events;
    
    private void OnTriggerStay()
    {
        ArrayList repeat_chances = new();
        float max = Mathf.Max(chances.ToArray());
        float min = Mathf.Min(chances.ToArray());
        int len_chances = chances.Length;
        float number_of_random;
        int index_find_number;
        int final_index;

        for (int i = 0; i < len_chances * 2; ++i)
        {
            random_value = UnityEngine.Random.value;

            if (random_value >= min)
            {
                number_of_random = FindNumberMax(chances, min, max, random_value);

                int count_random_number = chances.Count(x => x == number_of_random);

                if (count_random_number == 1)
                {
                    index_find_number = System.Array.IndexOf(chances, number_of_random);
                }
                else
                {
                    for (int i_random = 0; i_random < len_chances; i_random++)
                    {
                        if (chances[i_random] == number_of_random)
                        {
                            repeat_chances.Add(i_random);
                        }
                    }
                    float first = UnityEngine.Random.value;
                    index_find_number = (int)(count_random_number * first);
                    repeat_chances.Clear();
                }
                match_list[index_find_number]++;
            }
        }

        final_index = System.Array.IndexOf(match_list, match_list.Max());

        Instantiate(events[final_index], events[final_index].transform.position, Quaternion.identity);

        match_list = new int[match_list.Length];
    }

    private float FindNumberMax(float[] chances, float min, float max, float random_value) {
        chances = (float[])chances.Clone();

        float difference = 100;
        float cur_difference;
        float final_number = 0;

        if (random_value > max) {
            return max;
        }

        for (int i = 0; i < chances.Length; i++) {
            if (random_value <= min) {
                continue;
            }
            cur_difference = Mathf.Abs(random_value - chances[i]);
            if (cur_difference < difference) {
                difference = cur_difference;
                final_number = chances[i];
            }
        }

        return final_number;
    } 

    private void OnTriggerExit()
    {

    }
}
