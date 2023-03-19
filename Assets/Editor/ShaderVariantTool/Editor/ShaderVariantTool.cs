using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Globalization;

namespace GfxQA.ShaderVariantTool
{
    public class ShaderVariantTool : EditorWindow
    {
        private VisualElement root;
        private ScrollView mainScrollView;

        [MenuItem("Window/ShaderVariantTool")]
        public static void ShowWindow()
        {
            //ShaderVariantTool wnd = GetWindow<ShaderVariantTool>();
            ShaderVariantTool wnd = CreateWindow<ShaderVariantTool>(); //Allow to create more instances
            wnd.titleContent = new GUIContent("ShaderVariantTool");
            wnd.minSize = new Vector2(800, 700);
        }

        public void CreateGUI()
        {
            root = rootVisualElement;

            //If there is no CSV file available, show an message to hint people to make a build first
            if(CSVFileNames == null) GetCSVFileNames();
            if(CSVFileNames.Count == 0)
            {
                root.styleSheets.Add(Resources.Load<StyleSheet>("ShaderVariantTool_Style"));
                Label initialMessage = new Label("Build the player and reopen this tool.");
                initialMessage.AddToClassList("initial-message");
                root.Add(initialMessage);
                return;
            }

            //Culture Setting ==================================================================

            //Getting main layout
            VisualTreeAsset rootVisualTree = Resources.Load<VisualTreeAsset>("ShaderVariantTool_Main");
            rootVisualTree.CloneTree(root);
            mainScrollView = root.Q<ScrollView>();

            //Culture Setting ==================================================================

            //Read culture pref
            Helper.SetupCultureInfo();

            //DropdownList
            DropdownField cultureDropdown = root.Q<DropdownField>(name:"CultureField");
            cultureDropdown.choices = Helper.cinfo.Select(i => i.DisplayName).ToList();
            cultureDropdown.index = Helper.cinfo.IndexOf(Helper.culture);
            cultureDropdown.RegisterValueChangedCallback(v =>
            {
                Helper.UpdateCultureInfo(v.newValue);
            });

            //CSV buttons ==================================================================

            //Refresh CSV button
            Button refreshCSVButton = root.Q<Button>(name:"RefreshCSVButton");
            refreshCSVButton.RegisterCallback<PointerUpEvent>(e =>
            {
                GetCSVFileNames();
                csvDropDown.choices = CSVFileNames;
            });

            //CSV drop down list
            csvDropDown = root.Q<DropdownField>(name:"CSVDropDown");
            csvDropDown.choices = CSVFileNames;
            if(csvDropDown.index < 0)
            {
                csvDropDown.index = 0;
                selectedCSVFile = csvDropDown.value;
                UpdateCSVDataForGUI();
                prevSelectedCSVFile = selectedCSVFile;
            }
            csvDropDown.RegisterValueChangedCallback(v =>
            {
                selectedCSVFile = v.newValue;
                if(prevSelectedCSVFile != selectedCSVFile)
                {
                    UpdateCSVDataForGUI();
                    RefreshUI();
                    prevSelectedCSVFile = selectedCSVFile;
                }
            });

            //Show in explorer button
            Button showInExplorerButton = root.Q<Button>(name:"ShowInExplorerButton");
            showInExplorerButton.RegisterCallback<PointerUpEvent>(e =>
            {
                string path = selectedCSVFile;
                EditorUtility.RevealInFinder(path.Replace(@"/", @"\"));
            });

            //Build summary ================================================================

            openedFileLabel = root.Q<Label>(name:"OpenedFile");
            build_label_summary = root.Q<Label>(className:"summary-label-content");
            MakeLabelCopyable(build_label_summary);

            //Shader table ===================================================================

            shaderTableAsset = Resources.Load<VisualTreeAsset>("ShaderVariantTool_ShaderTable");
            shaderTableParent = root.Q<VisualElement>(className:"shader-table");

            //Fill the UI data ================================================================
            RefreshUI();
        }

        private void RefreshUI()
        {
            //Print the opened file name
            openedFileLabel.text = "Opened CSV file:" + selectedCSVFile;

            //Fill the build summary rows
            combinedBuildRowsText = "";
            for(int i=0; i<buildRows.Count; i++)
            {
                //Add some spaces
                if( buildRows[i][0] =="Shader Count" || buildRows[i][0] =="ComputeShader Count" || buildRows[i][0].Contains("Shader Program Count") )
                {
                    combinedBuildRowsText += "\n";
                }

                //Fill the table
                if( buildRows[i][0] !="Build Path" )//Do not show build path
                {
                    combinedBuildRowsText += String.Format( format, buildRows[i][0], Helper.NumberSeperator(buildRows[i][1]) );
                    combinedBuildRowsText += "\n";
                }
            }
            build_label_summary.text = combinedBuildRowsText;

            //Collapse shader table
            //shaderTable.CollapseAll();

            //Recreate ShaderTable ==========================================================
            if(shaderTable != null) shaderTableParent.Remove(shaderTable);
            shaderTableTemplate = shaderTableAsset.Instantiate();
            shaderTable = shaderTableTemplate.Q<MultiColumnTreeView>(name:"ShaderTable");
            shaderTableParent.Add(shaderTable);

            foreach(string col in shaderRowsHeaderFromCSV)
            {
                if(col.Contains("Program Count "))
                {
                    //Add more columns
                    Column newColumn = new Column();
                    newColumn.name = col;
                    newColumn.title = col;
                    newColumn.width = 200;
                    shaderTable.columns.Add(newColumn);
                }
            }

            /* To avoid mouse wheel conflict
            */
            ScrollView shaderTableScrollView = shaderTable.Q<ScrollView>();
            shaderTableScrollView.mode = ScrollViewMode.Vertical;
            shaderTableScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            
            //Register sorting event
            shaderTable.columnSortingChanged += OnShaderSortingChanged;
            
            //For show/hide variant table
            shaderTable.RegisterCallback<PointerUpEvent>(ShowHideVariantTable);

            //Refresh shader table
            SetDefaultSortingState(); //Set default sorting state
            OnShaderSortingChanged();
            shaderTable.Rebuild();

            //Refresh variant tables
            ResetExpandedVariantElementsList();

            SetShaderTableSelection();
        }

        private void MakeLabelCopyable(Label label)
        {
            ContextualMenuManipulator m = new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                //Make sure it's right click
                #if UNITY_2023_1_OR_NEWER
                    if(evt.triggerEvent.imguiEvent.keyCode == KeyCode.Mouse1)
                    {
                        evt.menu.AppendAction("Copy", (x) => EditorGUIUtility.systemCopyBuffer = label.text, DropdownMenuAction.AlwaysEnabled);
                    }
                #else
                    if(evt.triggerEvent.imguiEvent.isMouse && evt.triggerEvent.imguiEvent.button == 1)
                    {
                        evt.menu.AppendAction("Copy", (x) => EditorGUIUtility.systemCopyBuffer = label.text, DropdownMenuAction.AlwaysEnabled);
                    }
                #endif
            });
            m.target = label;
        }
    
#region CSV file reading functions
    
        private List<string> CSVFileNames;
        private string selectedCSVFile = "";
        private string prevSelectedCSVFile = "";
        private DropdownField csvDropDown;

        private Label openedFileLabel;

        private void GetCSVFileNames()
        {
            if(CSVFileNames == null)
            {
                CSVFileNames = new List<string>();
            }
            else
            {
                CSVFileNames.Clear();
            }

            var info = new DirectoryInfo(Helper.GetCSVFolderPath());
            var fileInfo = info.GetFiles();
            foreach(FileInfo file in fileInfo)
            {
                if(file.Name.Contains("ShaderVariants_") && file.FullName.Contains(".csv"))
                {
                    CSVFileNames.Add(file.FullName);
                }
            }

            //sort file names, top is newest
            CSVFileNames = CSVFileNames.OrderByDescending(o=>o).ToList();
        }

        private string[] ReadCSVFile()
        {
            string fileData  = System.IO.File.ReadAllText(selectedCSVFile);
            string[] lines = fileData.Split("\n"[0]);
            return lines;
        }

        private string[] GetLineCells(string[] lines, int lineID)
        {
            return lines[lineID].Trim().Split(","[0]);
        }

        void UpdateCSVDataForGUI()
        {
            //Read the CSV file lines
            string[] lines = ReadCSVFile();
            if(lines.Length <= 0)
            {
                //TODO some error messages when there is no line to read
            }
            else
            {
                int lineID = 0;
                string[] lineData = GetLineCells(lines,lineID);

                //Load build summary data
                if(buildRows == null) buildRows = new List<string[]>();
                buildRows.Clear();
                while(lineData[0]!="")
                {
                    buildRows.Add(lineData);

                    //Next line
                    lineID++;
                    lineData = GetLineCells(lines,lineID);
                }

                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Shader list header
                shaderRowsHeaderFromCSV = lineData;
                
                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Collect shader summary rows
                if(shaderRows == null) shaderRows = new List<string[]>();
                shaderRows.Clear();
                while(lineData[0]!="")
                {
                    shaderRows.Add(lineData);

                    //Next line
                    lineID++;
                    lineData = GetLineCells(lines,lineID);
                }

                //Sort shader rows for initial state
                if(shaderTable != null)
                {
                    SetDefaultSortingState();
                }

                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Variant list header
                //TODO: error check to see if uxml column matches CSV

                //Next line
                lineID++;
                lineData = GetLineCells(lines,lineID);

                //Collect variant rows
                if(variantRows == null) variantRows = new List<string[]>();
                variantRows.Clear();
                while(lineData[0]!="")
                {
                    variantRows.Add(lineData);

                    //Next line
                    lineID++;
                    lineData = GetLineCells(lines,lineID);
                }

                //Sort variant list by compile count
                //variantRows = variantRows.OrderByDescending(o=>int.Parse(o[defaultVariantSortColumn])).ToList();
                variantRows = variantRows.OrderByDescending(o=>int.Parse(o[defaultVariantSortColumn])).ThenBy(o=>o[3]).ToList();

                Debug.Log("ShaderVariantTool - successfully loaded "+selectedCSVFile);
            }
        }

#endregion
#region Build summary variables

        private List<string[]> buildRows;
        private string combinedBuildRowsText = "";
        private string format = "{0, -50}{1, 35}";
        private Label build_label_summary;

#endregion
#region Shader table functions

        private MultiColumnTreeView shaderTable;
        private List<TreeViewItemData<string[]>> shaderRoots;
        private List<string[]> shaderRows;
        private string[] shaderRowsHeaderFromCSV;
        private int defaultSortColumn = 4; //Sorting default is no. variant after stripping (i.e. variant in build)
        private VisualTreeAsset shaderTableAsset;
        private VisualElement shaderTableParent;
        private TemplateContainer shaderTableTemplate;

        private VisualElement CreateShaderTableCellLabel()
        {
            VisualElement el = new VisualElement();

            Label l = new Label();
            l.AddToClassList("table-label");
            MakeLabelCopyable(l);

            el.Add(l);

            return el;
        }

        //Add to list for setting selection state, so that it clears selection for collapsed items, otherwise table color is quite messy
        private void SetShaderTableSelection()
        {   
            var rootIds = shaderTable.GetRootIds().ToList();
            VisualElement shaderRowElement = shaderTable.GetRootElementForId(rootIds[0]);
            if(shaderRowElement == null) return;
            var rows = shaderRowElement.parent.Children();
            var listOfExpandedItems = new List<int>();
            for(int i=0; i<rows.Count(); i++)
            {
                int rowId = shaderTable.GetIdForIndex(i);
                bool rowIsRoot = rootIds.Contains(rowId);
                if(rowIsRoot && shaderTable.IsExpanded(rowId))
                {
                    rows.ElementAt(i).AddToClassList("shader-table-selected");
                    listOfExpandedItems.Add(i);
                }
                else
                {
                    rows.ElementAt(i).RemoveFromClassList("shader-table-selected");
                }
            }
            shaderTable.SetSelectionWithoutNotify(listOfExpandedItems);
        }

        private void SetShaderRootItems()
        {
            if(shaderRoots == null) shaderRoots = new List<TreeViewItemData<string[]>>(shaderRows.Count);
            shaderRoots.Clear();          

            int id = 0;
            for(int i=0; i<shaderRows.Count; i++)
            {
                //Add a dummy child to the shader, serve as a space for the variant table
                var dummyListTree = new List<TreeViewItemData<string[]>>(1);
                string[] dum = new string[shaderTable.columns.Count];
                for(int j = 0; j< shaderTable.columns.Count; j++)
                {
                    //using shaderName+dummy as identifer to recognise if the cell is variant table
                    string shaderName = "";
                    if(j==0)
                    {
                        shaderName = shaderRows[i][j];
                    }
                    dum[j] = shaderName+" dummy"+j;
                }
                dummyListTree.Add(new TreeViewItemData<string[]>(id++,dum));
                
                //Add shader cell contents
                string[] shaderRowCells = new string[shaderTable.columns.Count];
                for(int k=0; k<shaderRowCells.Length; k++)
                {
                    Column col = shaderTable.columns.ElementAt(k);
                    int CSVColumnId = Array.FindIndex(shaderRowsHeaderFromCSV, e => e == col.title);
                    
                    shaderRowCells[k] = shaderRows[i][CSVColumnId];
                }
                shaderRoots.Add(new TreeViewItemData<string[]>(id++, shaderRowCells, dummyListTree));
            }
            shaderTable.SetRootItems(shaderRoots);

            //Bind cell data to column
            Columns shaderTableColumns = shaderTable.columns;
            int colid = 0; //cannot use i inside bindCell as it is always = column count. colid will be increment to columnCount * shaderRowsCount
            for(int i=0; i<shaderTableColumns.Count; i++)
            {
                shaderTable.columns[i].makeCell = () => CreateShaderTableCellLabel();
                shaderTable.columns[i].bindCell = (VisualElement element, int index) =>
                {
                    int actualcolumn = colid % shaderTableColumns.Count;
                    string content = shaderTable.GetItemDataForIndex<string[]>(index)[actualcolumn];
                    Label titleLable = element.Q<Label>(className:"table-label");
                    titleLable.text = Helper.NumberSeperator(content);
                    colid++;

                    //Remove the variant tables as they need to get refreshed
                    VisualElement variantTemplateEL = element.Q(className:"VariantTemplate");
                    if(variantTemplateEL != null)
                    {
                        VisualElement templateContainer = variantTemplateEL.parent;
                        templateContainer.style.display = DisplayStyle.None;
                        templateContainer.RemoveFromHierarchy();
                    }

                    //Show the variant table
                    if(content.Contains("dummy0"))
                    {
                        //Get and show the variant table
                        string shaderName = content.Replace(" dummy"+actualcolumn,"");
                        VisualElement variantTable = GetVariantTable(shaderName);
                        variantTable.style.display = DisplayStyle.Flex;
                        element.Add(variantTable);

                        element.style.display = DisplayStyle.Flex;
                        titleLable.style.display = DisplayStyle.None; //dummy label

                        element.parent.parent.style.display = DisplayStyle.Flex;
                        element.parent.parent.AddToClassList("expanded-row-template");
                    }
                    //Hide the variant table and the dummy cells
                    else if(content.Contains("dummy"))
                    {
                        element.style.display = DisplayStyle.None;
                        titleLable.style.display = DisplayStyle.None; //dummy label

                        element.parent.parent.style.display = DisplayStyle.None;
                        element.parent.parent.RemoveFromClassList("expanded-row-template");
                    }
                    //Show the shader root row
                    else
                    {
                        element.style.display = DisplayStyle.Flex;
                        titleLable.style.display = DisplayStyle.Flex;

                        element.parent.parent.style.display = DisplayStyle.Flex;
                        element.parent.parent.RemoveFromClassList("expanded-row-template");
                    }
                };
            }
        }

        private void SetDefaultSortingState()
        {
            var sort = shaderTable.sortColumnDescriptions;

            if(sort.Count == 0)
            {
                SortColumnDescription defaultDesc = new SortColumnDescription(defaultSortColumn,SortDirection.Descending);
                sort.Add(defaultDesc);

                OnShaderSortingChanged();
            }
        }

        private void OnShaderSortingChanged()
        {
            shaderTable.CollapseAll();

            var sort = shaderTable.sortColumnDescriptions;

            //Sort based on sorting state
            for(int i=0; i<sort.Count; i++)
            {
                int columnId = shaderTable.columns.IndexOf(sort[i].column);

                if(columnId > 0) //First column is shader name, so sort by Alphabetical
                {
                    //sort numbers
                    if(sort[i].direction == SortDirection.Ascending)
                    {
                        shaderRows = shaderRows.OrderBy(o=>float.Parse(o[columnId])).ToList();
                    }
                    else
                    {
                        shaderRows = shaderRows.OrderByDescending(o=>float.Parse(o[columnId])).ToList();
                    }
                }
                else
                {
                    //sort string Alphabetical Order
                    if(sort[i].direction == SortDirection.Ascending)
                    {
                        shaderRows = shaderRows.OrderBy(o=>o[columnId]).ToList();
                    }
                    else
                    {
                        shaderRows = shaderRows.OrderByDescending(o=>o[columnId]).ToList();
                    }
                }

            }
            
            SetShaderRootItems();
            shaderTable.Rebuild();
            SetShaderTableSelection();
        }

#endregion
#region Variant table variables

        private List<string[]> variantRows;
        private int defaultVariantSortColumn = 2; //Sorting default is no. variant after stripping (i.e. variant in build)
        private List<VisualElement> expandedVariantElements;
        private VisualTreeAsset variantTableAsset;

        private Label CreateVariantTableCellLabel()
        {
            Label l = new Label();
            l.AddToClassList("table-label");
            MakeLabelCopyable(l);
            return l;
        }

        private void ResetExpandedVariantElementsList()
        {
            if(expandedVariantElements != null) expandedVariantElements.Clear();
            expandedVariantElements = new List<VisualElement>(shaderRows.Count);
            for(int i=0; i<shaderRows.Count; i++)
            {
                expandedVariantElements.Add(null);
            }
        }

        private void ShowHideVariantTable(PointerUpEvent e)
        {
            //Check if it is left click
            #if UNITY_2023_1_OR_NEWER
                if(e.imguiEvent.keyCode != KeyCode.Mouse0)
                {
                    return;
                }
            #else
                if(e.imguiEvent.isMouse && e.imguiEvent.button != 0)
                {
                    return;
                }
            #endif

            //Check if it's shader row
            int selectedIndex = shaderTable.selectedIndex;
            int selectedId = shaderTable.GetIdForIndex(selectedIndex);
            var rootIds = shaderTable.GetRootIds().ToList();
            bool isRootRow = rootIds.Contains(selectedId);
            if(isRootRow)
            {
                //toggle expand or collapse
                bool expanded = shaderTable.IsExpanded(selectedId);
                if(!expanded)
                {
                    //Expand
                    shaderTable.ExpandItem(selectedId);
                }
                else
                {
                    //Collapse
                    shaderTable.CollapseItem(selectedId);
                }
            }

            SetShaderTableSelection();
        }

        private VisualElement GetVariantTable(string shaderName)//int shaderId)
        {
            //string shaderName = shaderTable.GetItemDataForId<string[]>(shaderId)[0];
            int shaderRowsId = shaderRows.FindIndex(x => x[0] == shaderName);

            //Try to find the existing expanded Variant of that shader
            int matchingExpandedIndex = expandedVariantElements.FindIndex(x => x != null && x.name == shaderName);

            //There is no existing expanded Variant of that shader, so create a new one
            if(matchingExpandedIndex < 0 || expandedVariantElements[matchingExpandedIndex] == null)
            {
                //Create variant table for that shader
                if(variantTableAsset==null) variantTableAsset = Resources.Load<VisualTreeAsset>("ShaderVariantTool_VariantTable");
                TemplateContainer variantTableTemplate = variantTableAsset.Instantiate();

                //Show the shader summary numbers
                VisualElement shaderSummaryElement = variantTableTemplate.Q<VisualElement>(name:"ShaderSummary");
                Columns shaderTableColumns = shaderTable.columns;
                string combinedShaderSummaryText = shaderName + "\n";
                for(int j=1;j<shaderTableColumns.Count;j++)
                {
                    int CSVColumnId = Array.FindIndex(shaderRowsHeaderFromCSV, e => e == shaderTableColumns[j].title);
                    bool addPrefix = !shaderTableColumns[j].title.Contains("Program Count");
                    combinedShaderSummaryText += String.Format( format, (addPrefix? "Variant Count " : "" ) + shaderTableColumns[j].title, Helper.NumberSeperator(shaderRows[shaderRowsId][CSVColumnId]) );
                    combinedShaderSummaryText += "\n";
                }
                Label label_summary = shaderSummaryElement.Q<Label>(className:"summary-label-content");
                label_summary.text = combinedShaderSummaryText;
                MakeLabelCopyable(label_summary);

                //Fill variant table
                {
                    MultiColumnListView variantTable = variantTableTemplate.Q<MultiColumnListView>(name: "VariantTable");

                    //Hide some columns based on shader type
                    string isComputeShader = shaderRows[shaderRowsId][10]; //get num from BuildProcess
                    List<string> columnsToHide = new List<string>();
                    if(isComputeShader == "True")
                    {
                        columnsToHide.Add("ShaderName");
                        columnsToHide.Add("PassName");
                        columnsToHide.Add("PassType");
                        columnsToHide.Add("ShaderType");
                    }
                    else
                    {
                        columnsToHide.Add("ShaderName");
                        columnsToHide.Add("KernelName");
                    }

                    //Find all the variants belongs to this shader
                    var variantRoots = new List<string[]>();
                    for(int k=0; k<variantRows.Count; k++)
                    {
                        if(variantRows[k][0] == shaderName)
                        {
                            //Skip the column data we want to hide
                            List<string> data = new List<string>();
                            for (int j=0; j<variantRows[k].Length; j++)
                            {
                                string colName = variantTable.columns[j].name;
                                if( !columnsToHide.Contains(colName) )
                                {
                                    data.Add(variantRows[k][j]);
                                }
                            }
                            variantRoots.Add(data.ToArray());
                        }
                    }
                    variantTable.itemsSource = variantRoots;

                    //Hide some columns based on shader type
                    foreach(string col in columnsToHide)
                    {
                        variantTable.columns.Remove(variantTable.columns.First(o => o.name == col));
                    }

                    //Collection size of variant table
                    Label numOfItems = variantTableTemplate.Q<Label>(name: "CollectionSize");
                    numOfItems.text = "No. of items: "+ variantRoots.Count;
                    

                    //Bind cell data to column
                    Columns variantTableColumns = variantTable.columns;
                    int colid = 0;
                    for(int i=0; i<variantTableColumns.Count; i++)
                    {
                        variantTable.columns[i].makeCell = () => CreateVariantTableCellLabel();
                        variantTable.columns[i].bindCell = (VisualElement element, int index) =>
                        {
                            int actualcolumn = colid % variantTableColumns.Count;
                            string content = variantRoots[index][actualcolumn];
                            (element as Label).text = Helper.NumberSeperator(content);
                            colid++;
                        };
                    }

                    //Set default sorting state
                    // var sort = variantTable.sortColumnDescriptions;
                    // if(sort.Count == 0)
                    // {
                    //     SortColumnDescription defaultDesc = new SortColumnDescription(defaultVariantSortColumn,SortDirection.Descending);
                    //     sort.Add(defaultDesc);

                    //     OnVariantSortingChanged(variantTable);
                    // }

                    //Register sorting event
                    //variantTable.columnSortingChanged += () => OnVariantSortingChanged(variantTable);
                }

                //Add to list
                variantTableTemplate.name = shaderName;
                if(matchingExpandedIndex < 0) matchingExpandedIndex = shaderRowsId;
                expandedVariantElements[matchingExpandedIndex] = variantTableTemplate;
            }

            //Make the table visible
            expandedVariantElements[matchingExpandedIndex].style.display = DisplayStyle.Flex;

            return expandedVariantElements[matchingExpandedIndex];        
        }
        
        /*
        private void OnVariantSortingChanged(MultiColumnListView variantTable)
        {
            var sort = variantTable.sortColumnDescriptions;

            VisualElement container = variantTable.Q("unity-content-container");
            
            for(int i=0; i<sort.Count; i++)
            {
                int columnId = variantTable.columns.IndexOf(sort[i].column);
                container.Sort(delegate (VisualElement e1, VisualElement e2)
                {
                    VisualElement e1ColumnCell = e1.Children().ElementAt(columnId);
                    Label e1Label = e1.Q<Label>();

                    VisualElement e2ColumnCell = e2.Children().ElementAt(columnId);
                    Label e2Label = e2.Q<Label>();

                    if(sort[i].direction == SortDirection.Ascending)
                    {
                        return e1Label.text.CompareTo(e2Label.text);
                    }
                    else
                    {
                        return e2Label.text.CompareTo(e1Label.text);
                    }
                });
            }


            //variantTable.MarkDirtyRepaint();//.Rebuild();

            // //Sort based on sorting state
            // for(int i=0; i<sort.Count; i++)
            // {
            //     int columnId = variantTable.columns.IndexOf(sort[i].column);

            //     if(columnId > 0) //First column is shader name, so sort by Alphabetical
            //     {
            //         //sort numbers
            //         if(sort[i].direction == SortDirection.Ascending)
            //         {
            //             shaderRows = shaderRows.OrderBy(o=>float.Parse(o[columnId])).ToList();
            //         }
            //         else
            //         {
            //             shaderRows = shaderRows.OrderByDescending(o=>float.Parse(o[columnId])).ToList();
            //         }
            //     }
            //     else
            //     {
            //         //sort string Alphabetical Order
            //         if(sort[i].direction == SortDirection.Ascending)
            //         {
            //             shaderRows = shaderRows.OrderBy(o=>o[columnId]).ToList();
            //         }
            //         else
            //         {
            //             shaderRows = shaderRows.OrderByDescending(o=>o[columnId]).ToList();
            //         }
            //     }

            // }
            
            // SetShaderRootItems();
        }
        */

#endregion
    
    }

    //===================================================================================================
    public static class SVL
    {
        //build process indicators
        public static string buildProcessIDTitleStart = "ShaderVariantTool_Start:";
        public static string buildProcessIDTitleEnd = "ShaderVariantTool_End:";
        public static string buildProcessID = ""; //For recognising position in EditorLog
        public static bool buildProcessStarted = false;

        //the big total
        public static double buildTime = 0;
        
        //data
        public static List<ShaderItem> shaderlist = new List<ShaderItem>();
        public static string[] columns;
        
        public static void ReadUXMLForColumns()
        {
            //Get columns from UXML
            if(columns == null)
            {
                VisualTreeAsset rootVisualTree = Resources.Load<VisualTreeAsset>("ShaderVariantTool_VariantTable");
                TemplateContainer variantTemplate = rootVisualTree.Instantiate();
                MultiColumnListView variantTable = variantTemplate.Q<MultiColumnListView>(name: "VariantTable");
                Columns variantTableColumns = variantTable.columns;
                columns = new string[variantTableColumns.Count];
                for(int i=0; i<columns.Length; i++)
                {
                    columns[i] = variantTableColumns[i].title;
                }
            }
        }

        public static void ResetBuildList()
        {
            if(!buildProcessStarted)
            {
                ReadUXMLForColumns();

                shaderlist.Clear();
                buildTime = 0;

                //For reading EditorLog, we can extract the contents
                buildProcessID = System.DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, buildProcessIDTitleStart+buildProcessID);
                
                buildTime = EditorApplication.timeSinceStartup;
                buildProcessStarted = true;
            }
        }
    }
}

