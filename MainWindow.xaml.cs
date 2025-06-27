using GraphMolWrap;
using Microsoft.Win32;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace sdf2smilesUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _sdfFilePath;
        private List<ROMol> _molecules;
        private BackgroundWorker _worker;
        public MainWindow()
        {
            InitializeComponent();
            _molecules = new List<ROMol>();
            _worker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };
            _worker.DoWork += Worker_DoWork;
            _worker.ProgressChanged += Worker_ProgressChanged;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void SelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "SDF files (*.sdf)|*.sdf",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _sdfFilePath = openFileDialog.FileName;
                try
                {
                    _molecules.Clear();
                    SMILESListBox.Items.Clear();
                    using (var reader = new SDMolSupplier(_sdfFilePath))
                    {
                        while (!reader.atEnd())
                        {
                            var mol = reader.next();
                            if (mol != null)
                            {
                                _molecules.Add(mol);
                            }
                        }
                    }
                    MoleculeCountTextBlock.Text = $"Molecules found: {_molecules.Count}";
                    ConvertButton.IsEnabled = _molecules.Count > 0;
                    CopyButton.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading SDF file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    MoleculeCountTextBlock.Text = "Molecules found: 0";
                    ConvertButton.IsEnabled = false;
                    CopyButton.IsEnabled = false;
                }
            }
        }

        private void ConvertSMILES_Click(object sender, RoutedEventArgs e)
        {
            if (_molecules.Count == 0)
            {
                MessageBox.Show("No molecules to convert.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConvertButton.IsEnabled = false;
            ConversionProgressBar.Value = 0;
            SMILESListBox.Items.Clear();
            CopyButton.IsEnabled = false;

            _worker.RunWorkerAsync(_molecules);
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var molecules = (List<ROMol>)e.Argument;
            var smilesList = new List<string>();
            int totalMolecules = molecules.Count;

            for (int i = 0; i < totalMolecules; i++)
            {
                try
                {
                    string smiles = RDKFuncs.MolToSmiles(molecules[i]);
                    if (!string.IsNullOrEmpty(smiles))
                    {
                        smilesList.Add(smiles);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Error converting molecule {i + 1}: {ex.Message}");
                }

                // Simulate 10ms per molecule
                Thread.Sleep(10);

                // Report progress
                int progress = (i + 1) * 100 / totalMolecules;
                _worker.ReportProgress(progress);
            }

            e.Result = smilesList;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ConversionProgressBar.Value = e.ProgressPercentage;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show($"Error during conversion: {e.Error.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ConvertButton.IsEnabled = true;
                return;
            }

            var smilesList = (List<string>)e.Result;
            foreach (var smiles in smilesList)
            {
                SMILESListBox.Items.Add(smiles);
            }

            ConvertButton.IsEnabled = true;
            CopyButton.IsEnabled = SMILESListBox.Items.Count > 0;
            //MessageBox.Show("SMILES conversion completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var smilesList = new List<string>();
                foreach (var item in SMILESListBox.Items)
                {
                    smilesList.Add(item.ToString());
                }
                Clipboard.SetText(string.Join("\n", smilesList));
                MessageBox.Show("SMILES copied to clipboard! Paste into Excel.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}