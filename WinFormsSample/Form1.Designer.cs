namespace WinFormsSample
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnArchivo = new Button();
            SuspendLayout();
            // 
            // btnArchivo
            // 
            btnArchivo.Location = new Point(54, 39);
            btnArchivo.Name = "btnArchivo";
            btnArchivo.Size = new Size(283, 57);
            btnArchivo.TabIndex = 0;
            btnArchivo.Text = "Cargar Archivo";
            btnArchivo.UseVisualStyleBackColor = true;
            btnArchivo.Click += btnArchivo_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnArchivo);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button btnArchivo;
    }
}
