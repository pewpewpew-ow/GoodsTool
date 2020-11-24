﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GoodsToolReworked.Structure
{
    class ProductsReader
    {
        public delegate void StoreHandler(Store store);
        public event StoreHandler ScanDone;

        public DateTime taskStarted;

        /// <summary>
        /// Полный путь к текстовому файлу.
        /// </summary>
        public static string Filename { get; set; }

        public static List<Store> Stores { get; set; }

        public ProductsReader(string filename, List<Store> storelist) 
        {
            Filename = filename;
            Stores = storelist;
            Read();
        }

        public async void Read()
        {
            taskStarted = DateTime.Now;
            using (var reader = new StreamReader(File.Open(Filename, FileMode.Open, FileAccess.Read)))
            {
                string text = reader.ReadToEnd();   //Весь текст
                string[] rows = text.Split('\n');   //Строки
                

                for (int i = 0; i < Stores.Count; i++)
                {
                    await Task.Run(() => CheckStore(rows, i));
                    
                }
                System.Windows.Forms.MessageBox.Show($"DONE in {(DateTime.Now - taskStarted).TotalSeconds}s.");
                //  TODO: Распитсать код с EclelMerge в Form1 (метод Worker) 
                //  Sol: Кол-во потоков = кол-во магазинов, пойдём в многопоточность, получается)))
                //  ID есть только у модели, у товаров их нет

            }
        }

        private static Model model;
        private void CheckStore(string[] rows, int storeID)
        {
            taskStarted = DateTime.Now;
            int strCount = rows.Length; //  Кол-во строк в текстовом док-те...
            //int strCount = 100;
            int Indexator = 0;          //  Индексатор моделей.

            //  С 14 строки т.к. юзлес инфо в начале документа.
            for (int i = 14; i < strCount; i++)
            {
                string[] columns = rows[i].Split('\t');         //  Столбцы.
                string[] attributes = columns[0].Split(',');    //  Размер и цвет

                //Если в ячейке нет запятых и она не пустая, то это модель.
                bool IsModel = (attributes[0] != string.Empty) && (attributes.Length == 1) ? true : false;
                if (IsModel)
                {
                    //  Не учитываем подарочные сертификаты.
                    if (attributes[0].ToLower().Contains("сертификат") || IsDigitsOnly(attributes[0])) continue;

                    if (model != null) 
                    { 
                        Stores[storeID].Models.Add(model);
                        if (model.Count != 0) Stores[storeID].AvailableModelsCount++;
                    }  //  Если мы встретили новую модель, записываем результаты прошлой

                    model = new Model(Indexator);
                    model.Name = attributes[0].Trim();  //  !Убираем пробелы в начале и в конце названия!
                    model.Id = Indexator++;             //  Присваиваем id постинкрементом
                }
                else
                {
                    string size = "BUG!"; string color = "BUG";
                    if (attributes.Length == 2)
                    {
                        size = attributes[0].Trim();     //  Size, Color (splitted)
                        color = attributes[1].Trim();    //  !Убираем пробелы в начале и в конце названия!
                    }
                    int count = columns[storeID + 1] != "" ? Convert.ToInt32(columns[storeID + 1].Trim().Replace(" ", "").Split(',')[0]) : -1;
                    int price = columns[storeID + 2] != "" ? Convert.ToInt32(columns[storeID + 2].Trim().Replace(" ", "").Split(',')[0]) : -1;
 
                    Product product = new Product(size, color, count, price);
                    if (count != -1 && price != -1)
                    {
                        model.Count++;
                        model.Products.Add(product);
                    }
                }
            }
            ScanDone(Stores[storeID]); //   Уведомление о том, что текущий магазин спарсился
        }

        private static bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

    }
}
