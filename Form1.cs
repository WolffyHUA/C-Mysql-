using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Access_MySQL
{
    public partial class Form1 : Form
    {
        private string currentTable = "student"; // 新增字段存储当前表名

        // 新增菜单项点击事件
        private void SwitchTable(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            currentTable = menuItem.Tag.ToString();
            button1_Click(null, null); // 自动刷新数据
        }

        public Form1()
        {
            InitializeComponent();
            dataGridView1.CellDoubleClick += dataGridView1_CellDoubleClick;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string str = "server=localhost;User Id=root;password=123456;Database=mydb"; //连接字符串
                MySqlConnection conn = new MySqlConnection(str); //实例化连接
                string sql = $"select * from {currentTable}";

                var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // 开始计时

                MySqlDataAdapter da = new MySqlDataAdapter(sql, conn); //数据适配器
                DataTable dt = new DataTable(); //数据表
                da.Fill(dt); //执行sql，将数据填入数据表

                stopwatch.Stop(); // 停止计时
                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds; // 获取用时

                dataGridView1.DataSource = dt; //将数据表绑定到dataGridView1上，显示出来

                // 在界面中显示查询结果信息
                Label resultLabel = new Label
                {
                    Text = $"查询到 {dt.Rows.Count} 条数据，用时 {elapsedSeconds:F2} 秒！",
                    AutoSize = true,
                    ForeColor = Color.Green,
                    Font = new Font("微软雅黑", 14, FontStyle.Regular),
                    Location = new Point(
                        (this.ClientSize.Width - 300) / 2,
                        dataGridView1.Bottom + 5
                    ), // 水平方向居中
                };

                // 如果已经存在结果标签，先移除
                var existingLabel = this
                    .Controls.OfType<Label>()
                    .FirstOrDefault(l => l.Text.StartsWith("查询到"));
                if (existingLabel != null)
                {
                    this.Controls.Remove(existingLabel);
                }

                this.Controls.Add(resultLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button2_Click_1(object sender, EventArgs e) // 删除操作
        {
            try
            {
                int row = dataGridView1.CurrentRow.Index;
                if (row < 0)
                    return;

                DialogResult result = MessageBox.Show(
                    "确实要删除吗？",
                    "询问信息",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (result == DialogResult.No)
                    return;

                // 获取整行数据
                var rowData = new Dictionary<string, object>();
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    rowData[column.Name] = dataGridView1[column.Index, row].Value;
                }

                string primaryKeyColumn = dataGridView1.Columns[0].Name; // 假设第一列为主键
                string primaryKeyValue = dataGridView1[0, row].Value.ToString();

                string deleteSql =
                    $"DELETE FROM {currentTable} WHERE {primaryKeyColumn} = @primaryKeyValue";
                var deleteParameters = new Dictionary<string, object>
                {
                    { "@primaryKeyValue", primaryKeyValue },
                };

                // 构建反向操作（插入整行数据）
                string reverseSql =
                    $"INSERT INTO {currentTable} ({string.Join(",", rowData.Keys)}) VALUES ({string.Join(",", rowData.Keys.Select(k => $"@{k}"))})";
                var reverseParameters = rowData.ToDictionary(
                    kvp => $"@{kvp.Key}",
                    kvp => kvp.Value
                );

                ExecuteNonQuery(deleteSql, deleteParameters);

                // 记录操作到撤销栈
                undoStack.Push(
                    new DatabaseOperation
                    {
                        Sql = deleteSql,
                        Parameters = deleteParameters,
                        ReverseSql = reverseSql,
                        ReverseParameters = reverseParameters,
                    }
                );
                redoStack.Clear(); // 清空恢复栈

                MessageBox.Show("删除成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // 刷新数据
            button1_Click(null, null);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                // Create a new form for data input
                Form inputForm = new Form();
                inputForm.Text = "新增数据";
                inputForm.Size = new Size(400, 300);

                // Create a panel to hold input fields
                Panel panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                inputForm.Controls.Add(panel);

                // Center the form on the screen
                inputForm.StartPosition = FormStartPosition.CenterScreen;

                // Get column names from the DataGridView
                List<TextBox> textBoxes = new List<TextBox>();
                int y = 10;
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    Label label = new Label
                    {
                        Text = column.HeaderText,
                        Location = new Point(10, y),
                        AutoSize = true,
                    };
                    panel.Controls.Add(label);

                    TextBox textBox = new TextBox
                    {
                        Name = column.Name,
                        Location = new Point(150, y),
                        Width = 200,
                    };
                    textBoxes.Add(textBox);
                    panel.Controls.Add(textBox);

                    y += 30;
                }

                // Add a button to confirm
                Button confirmButton = new Button
                {
                    Text = "确认",
                    Location = new Point(150, y + 10),
                    Width = 100,
                };
                panel.Controls.Add(confirmButton);

                // Add a button to fill example data
                Button fillExampleButton = new Button
                {
                    Text = "随机数据",
                    Location = new Point(260, y + 10),
                    Width = 100,
                };
                panel.Controls.Add(fillExampleButton);

                fillExampleButton.Click += (s, args) =>
                {
                    Random random = new Random();
                    foreach (TextBox textBox in textBoxes)
                    {
                        if (
                            textBox.Name.ToLower().Contains("id")
                            || textBox.Name.ToLower().Contains("no")
                        )
                        {
                            textBox.Text = random.Next(1000, 9999).ToString(); // Generate random numeric ID
                        }
                        else if (textBox.Name.ToLower().Contains("name"))
                        {
                            textBox.Text = "Name" + random.Next(1, 100); // Generate random name
                        }
                        else if (textBox.Name.ToLower().Contains("date"))
                        {
                            textBox.Text = DateTime
                                .Now.AddDays(random.Next(-1000, 1000))
                                .ToString("yyyy-MM-dd"); // Generate random date
                        }
                        else if (textBox.Name.ToLower().Contains("age"))
                        {
                            textBox.Text = random.Next(18, 60).ToString(); // Generate random age
                        }
                        else if (textBox.Name.ToLower().Contains("ssex"))
                        {
                            textBox.Text = random.Next(0, 2) == 0 ? "男" : "女"; // Generate random gender
                        }
                        else
                        {
                            textBox.Text = "Example" + random.Next(1, 100); // Default example text
                        }
                    }
                };

                confirmButton.Click += (s, args) =>
                {
                    try
                    {
                        string str =
                            "server=localhost;User Id=root;password=123456;Database=mydb";
                        MySqlConnection conn = new MySqlConnection(str);
                        conn.Open();

                        // Build the INSERT SQL query
                        string columns = string.Join(
                            ",",
                            dataGridView1.Columns.Cast<DataGridViewColumn>().Select(c => c.Name)
                        );
                        string values = string.Join(",", textBoxes.Select(tb => $"'{tb.Text}'"));
                        string sql = $"INSERT INTO {currentTable} ({columns}) VALUES ({values})";

                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        int count = cmd.ExecuteNonQuery();
                        conn.Close();

                        MessageBox.Show($"成功增加 {count} 行！");
                        inputForm.Close();

                        // Refresh the DataGridView
                        button1_Click(null, null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                };

                inputForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // Create a new form for input
                Form inputForm = new Form();
                inputForm.Text = "查找数据";
                inputForm.Size = new Size(300, 150);

                // Center the form on the screen
                inputForm.StartPosition = FormStartPosition.CenterScreen;

                Label label = new Label
                {
                    Text = "输入关键词：",
                    Location = new Point(10, 20),
                    AutoSize = true,
                };
                inputForm.Controls.Add(label);

                TextBox textBox = new TextBox { Location = new Point(10, 50), Width = 250 };
                inputForm.Controls.Add(textBox);

                Button searchButton = new Button
                {
                    Text = "查找",
                    Location = new Point(10, 80),
                    Width = 100,
                };
                inputForm.Controls.Add(searchButton);

                searchButton.Click += (s, args) =>
                {
                    try
                    {
                        string searchTerm = textBox.Text.Trim();
                        if (string.IsNullOrEmpty(searchTerm))
                        {
                            MessageBox.Show("请输入关键词");
                            return;
                        }

                        string str =
                            "server=localhost;User Id=root;password=123456;Database=mydb";
                        MySqlConnection conn = new MySqlConnection(str);
                        string sql =
                            $"SELECT * FROM student WHERE CONCAT_WS('', {string.Join(",", dataGridView1.Columns.Cast<DataGridViewColumn>().Select(c => c.Name))}) LIKE '%{searchTerm}%'";
                        MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridView1.DataSource = dt;

                        inputForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                };

                inputForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                // Create a new form for SQL input
                Form sqlInputForm = new Form();
                sqlInputForm.Text = "自定义 SQL 查询";
                sqlInputForm.Size = new Size(500, 300);

                // Center the form on the screen
                sqlInputForm.StartPosition = FormStartPosition.CenterScreen;

                Label label = new Label
                {
                    Text = "输入 SQL 查询语句：",
                    Location = new Point(10, 20),
                    AutoSize = true,
                };
                sqlInputForm.Controls.Add(label);

                TextBox sqlTextBox = new TextBox
                {
                    Location = new Point(10, 50),
                    Width = 460,
                    Height = 150,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                };
                sqlInputForm.Controls.Add(sqlTextBox);

                Button executeButton = new Button
                {
                    Text = "执行",
                    Location = new Point(10, 220),
                    Width = 100,
                };
                sqlInputForm.Controls.Add(executeButton);

                executeButton.Click += (s, args) =>
                {
                    try
                    {
                        string sqlQuery = sqlTextBox.Text.Trim();
                        if (string.IsNullOrEmpty(sqlQuery))
                        {
                            MessageBox.Show("请输入 SQL 查询语句");
                            return;
                        }

                        string str =
                            "server=localhost;User Id=root;password=123456;Database=mydb";
                        MySqlConnection conn = new MySqlConnection(str);

                        var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // 开始计时

                        MySqlDataAdapter da = new MySqlDataAdapter(sqlQuery, conn);
                        DataTable dt = new DataTable();
                        da.Fill(dt);

                        stopwatch.Stop(); // 停止计时
                        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds; // 获取用时

                        dataGridView1.DataSource = dt;

                        // 在界面中显示查询结果信息
                        Label resultLabel = new Label
                        {
                            Text = $"查询到 {dt.Rows.Count} 条数据，用时 {elapsedSeconds:F2} 秒！",
                            AutoSize = true,
                            ForeColor = Color.Green,
                            Font = new Font("微软雅黑", 14, FontStyle.Regular),
                            Location = new Point(
                                (this.ClientSize.Width - 300) / 2,
                                dataGridView1.Bottom + 5
                            ), // 水平方向居中
                        };

                        // 如果已经存在结果标签，先移除
                        var existingLabel = this
                            .Controls.OfType<Label>()
                            .FirstOrDefault(l => l.Text.StartsWith("查询到"));
                        if (existingLabel != null)
                        {
                            this.Controls.Remove(existingLabel);
                        }

                        this.Controls.Add(resultLabel);

                        sqlInputForm.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"执行查询时出错：{ex.Message}");
                    }
                };

                sqlInputForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                // 获取当前单元格的值和列名
                string columnName = dataGridView1.Columns[e.ColumnIndex].Name;
                string originalValue = dataGridView1[e.ColumnIndex, e.RowIndex].Value?.ToString();

                // 创建修改窗口
                Form editForm = new Form();
                editForm.Text = "修改数据";
                editForm.Size = new Size(300, 200);
                editForm.StartPosition = FormStartPosition.CenterScreen;

                Label label = new Label
                {
                    Text = $"修改 {columnName}：",
                    Location = new Point(10, 20),
                    AutoSize = true,
                };
                editForm.Controls.Add(label);

                TextBox textBox = new TextBox
                {
                    Text = originalValue,
                    Location = new Point(10, 50),
                    Width = 250,
                };
                editForm.Controls.Add(textBox);

                Button confirmButton = new Button
                {
                    Text = "确认",
                    Location = new Point(10, 100),
                    Width = 100,
                };
                editForm.Controls.Add(confirmButton);

                confirmButton.Click += (s, args) =>
                {
                    try
                    {
                        string newValue = textBox.Text.Trim();
                        if (newValue == originalValue)
                        {
                            MessageBox.Show("值未改变");
                            return;
                        }

                        // 获取主键列及其值
                        string primaryKeyColumn = dataGridView1.Columns[0].Name; // 假设第一列为主键
                        string primaryKeyValue = dataGridView1[0, e.RowIndex].Value.ToString();

                        string str =
                            "server=localhost;User Id=root;password=123456;Database=mydb";
                        MySqlConnection conn = new MySqlConnection(str);
                        conn.Open();

                        // 构建 UPDATE SQL 语句
                        string sql =
                            $"UPDATE {currentTable} SET {columnName} = @newValue WHERE {primaryKeyColumn} = @primaryKeyValue";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@newValue", newValue);
                        cmd.Parameters.AddWithValue("@primaryKeyValue", primaryKeyValue);

                        // 构建反向操作
                        string reverseSql =
                            $"UPDATE {currentTable} SET {columnName} = @originalValue WHERE {primaryKeyColumn} = @primaryKeyValue";
                        var reverseParameters = new Dictionary<string, object>
                        {
                            { "@originalValue", originalValue },
                            { "@primaryKeyValue", primaryKeyValue },
                        };

                        cmd.ExecuteNonQuery();
                        conn.Close();

                        // 记录操作到撤销栈
                        undoStack.Push(
                            new DatabaseOperation
                            {
                                Sql = sql,
                                Parameters = new Dictionary<string, object>
                                {
                                    { "@newValue", newValue },
                                    { "@primaryKeyValue", primaryKeyValue },
                                },
                                ReverseSql = reverseSql,
                                ReverseParameters = reverseParameters,
                            }
                        );
                        redoStack.Clear(); // 清空恢复栈

                        editForm.Close();

                        // 刷新数据
                        button1_Click(null, null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"修改失败：{ex.Message}");
                    }
                };

                editForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}");
            }
        }

        // 定义操作记录类
        public class DatabaseOperation
        {
            public string Sql { get; set; }
            public Dictionary<string, object> Parameters { get; set; }
            public string ReverseSql { get; set; }
            public Dictionary<string, object> ReverseParameters { get; set; }
        }

        // 在 Form1 类中新增字段
        private Stack<DatabaseOperation> undoStack = new Stack<DatabaseOperation>();
        private Stack<DatabaseOperation> redoStack = new Stack<DatabaseOperation>();

        // 修改撤销功能
        private void Undo(object sender, EventArgs e)
        {
            if (undoStack.Count == 0)
            {
                MessageBox.Show("没有可以撤销的操作！");
                return;
            }

            var operation = undoStack.Pop();
            ExecuteNonQuery(operation.ReverseSql, operation.ReverseParameters);
            redoStack.Push(operation);
            MessageBox.Show("撤销成功！");
            button1_Click(null, null); // 刷新数据
        }

        // 修改恢复功能
        private void Redo(object sender, EventArgs e)
        {
            if (redoStack.Count == 0)
            {
                MessageBox.Show("没有可以恢复的操作！");
                return;
            }

            var operation = redoStack.Pop();
            ExecuteNonQuery(operation.Sql, operation.Parameters);
            undoStack.Push(operation);
            MessageBox.Show("恢复成功！");
            button1_Click(null, null); // 刷新数据
        }

        // 执行非查询 SQL 的通用方法
        private void ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
        {
            try
            {
                string str = "server=localhost;User Id=root;password=123456;Database=mydb";
                using (MySqlConnection conn = new MySqlConnection(str))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                            {
                                cmd.Parameters.AddWithValue(param.Key, param.Value);
                            }
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}");
            }
        }
    }
}
