using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Xml.Serialization;
using Ascon.Pilot.SDK.Controls;
using Ascon.Pilot.SDK.Menu;
using Ascon.Pilot.SDK.Toolbar;
using Ascon.Pilot.Theme.ColorScheme;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace Ascon.Pilot.SDK.GraphicLayerSample
{
    static class IconLoader
    {
        public static byte[] GetIcon(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = $"Ascon.Pilot.SDK.GraphicLayerSample.Icons.{resourceName}";

            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null) return null;
                return ReadBytes(stream);
            }
        }

        private static byte[] ReadBytes(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }

    // === ОСНОВНОЙ КЛАСС ПЛАГИНА ===
    [Export(typeof(IMenu<MainViewContext>))]
    [Export(typeof(IToolbar<XpsRenderContext>))]
    public class GraphicLayerToolbar : IMenu<MainViewContext>, IToolbar<XpsRenderContext>, IMouseLeftClickListener
    {
        private const string ServiceGraphicLayerMenu = "ServiceGraphicLayerMenu";
        private const string TbiAddStamp = "tbiAddStampButton_Unique"; // Уникальный ID кнопки

        private readonly IObjectModifier _modifier;
        private readonly IObjectsRepository _repository;
        private readonly IXpsViewer _xpsViewer;

        private IPerson _currentPerson;
        private GraphicLayerElementSettingsView _settingsView;
        private GraphicLayerElementSettingsModel _model;

        private string _filePath = string.Empty;
        private double _scaleXY;
        private double _angle;
        private bool _includeStamp;

        // Флаг: включен ли режим штампа
        private bool _isStampModeActive = false;
        private IToolbarBuilder _toolbarBuilder;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [ImportingConstructor]
        public GraphicLayerToolbar(IObjectModifier modifier, IObjectsRepository repository, IPilotDialogService dialogService, IXpsViewer xpsViewer)
        {
            _modifier = modifier;
            _repository = repository;
            _xpsViewer = xpsViewer;

            var convertFromString = ColorConverter.ConvertFromString(dialogService.AccentColor);
            if (convertFromString != null)
            {
                var color = (Color)convertFromString;
                ColorScheme.Initialize(color, dialogService.Theme);
            }

            CheckSettings();
        }

        #region Меню настроек (IMenu)

        public void Build(IMenuBuilder builder, MainViewContext context)
        {
            var menuItem = builder.ItemNames.First();
            builder.GetItem(menuItem).AddItem(ServiceGraphicLayerMenu, 0).WithHeader(Properties.Resources.txtMenuItem);
        }

        public void OnMenuItemClick(string itemName, MainViewContext context)
        {
            if (itemName != ServiceGraphicLayerMenu) return;

            var scale = _scaleXY.ToString("N1");
            var angle = _angle.ToString(CultureInfo.InvariantCulture);

            _model = new GraphicLayerElementSettingsModel(_filePath, scale, angle, _includeStamp);
            _model.OnSaveSettings += ReloadSettings;

            _settingsView = new GraphicLayerElementSettingsView { DataContext = _model };

            var activeWindowHandle = GetForegroundWindow();
            new WindowInteropHelper(_settingsView)
            {
                Owner = activeWindowHandle
            }.Owner = activeWindowHandle;

            _settingsView.ShowDialog();
        }

        private void ReloadSettings(object sender, EventArgs e)
        {
            CheckSettings();
        }

        private void CheckSettings()
        {
            _filePath = Properties.Settings.Default.Path;
            _includeStamp = Properties.Settings.Default.IncludeStamp;
            _scaleXY = 1;

            try
            {
                var tmp = Properties.Settings.Default.Scale.Split(',', '.');
                double.TryParse(tmp[0], out var whole);
                double fraction = 0;
                if (tmp.Length > 1)
                    double.TryParse("0" + CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator + tmp[1], out fraction);
                _scaleXY = whole + fraction;
            }
            catch { }

            double.TryParse(Properties.Settings.Default.Angle, out _angle);
        }

        #endregion

        #region Панель инструментов (IToolbar)

        public void Build(IToolbarBuilder builder, XpsRenderContext context)
        {
            _toolbarBuilder = builder;

            // Вызываем метод, который либо создаст кнопку, либо обновит существующую
            AddOrUpdateStampButton(context.IsDocumentLoaded);
        }

        public void OnToolbarItemClick(string name, XpsRenderContext context)
        {
            if (name == TbiAddStamp)
            {
                // Переключаем режим (Вкл <-> Выкл)
                if (_isStampModeActive)
                    DeactivateStampMode();
                else
                    ActivateStampMode();
            }
        }

        /// <summary>
        /// Универсальный метод для создания или обновления кнопки.
        /// Гарантирует, что кнопка будет одна.
        /// </summary>
        private void AddOrUpdateStampButton(bool isEnabled)
        {
            if (_toolbarBuilder == null) return;

            var iconBytes = IconLoader.GetIcon("add_stamp.svg");

            // Меняем подсказку в зависимости от режима
            var hint = _isStampModeActive
                ? "Режим штампа АКТИВЕН. Кликайте по листу для вставки. Нажмите кнопку еще раз для выхода."
                : "Вставить штамп (Нажмите, чтобы включить режим)";

            // ПРОВЕРКА: Если кнопка уже есть в списке имен...
            if (_toolbarBuilder.ItemNames.Contains(TbiAddStamp))
            {
                // ... мы её ЗАМЕНЯЕМ (обновляем состояние)
                _toolbarBuilder.ReplaceToggleButtonItem(TbiAddStamp)
                    .WithIsChecked(_isStampModeActive) // Визуально: нажата или нет
                    .WithIcon(iconBytes)
                    .WithHint(hint)
                    .WithIsEnabled(isEnabled);
            }
            else
            {
                // ... если нет, добавляем НОВУЮ в конец списка (builder.Count)
                _toolbarBuilder.AddToggleButtonItem(TbiAddStamp, _toolbarBuilder.Count)
                    .WithIsChecked(_isStampModeActive)
                    .WithIcon(iconBytes)
                    .WithHint(hint)
                    .WithIsEnabled(isEnabled);
            }
        }

        private void ActivateStampMode()
        {
            _isStampModeActive = true;
            // Подписываемся на клик левой кнопкой мыши
            _xpsViewer.SubscribeLeftMouseClick(this);
            // Обновляем вид кнопки (делаем её нажатой)
            AddOrUpdateStampButton(true);
        }

        private void DeactivateStampMode()
        {
            _isStampModeActive = false;
            // Отписываемся от клика
            _xpsViewer.UnsubscribeLeftMouseClick(this);
            // Обновляем вид кнопки (делаем её отжатой)
            AddOrUpdateStampButton(true);
        }

        #endregion

        #region Обработка клика мыши (IMouseLeftClickListener)

        public void OnLeftMouseButtonClick(XpsRenderClickPointContext pointContext)
        {
            // 1. Проверяем настройки перед вставкой
            CheckSettings();

            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                MessageBox.Show("Файл штампа не найден. Проверьте настройки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                DeactivateStampMode(); // Если ошибка — выключаем режим
                return;
            }

            _currentPerson = _repository.GetCurrentPerson();
            // 2. ПРОВЕРКА ПРАВ: Может ли текущий пользователь ставить штамп?
            if (!CanUserAddStamp(pointContext.DataObject))
            {
                MessageBox.Show("У вас нет прав на вставку штампа. Ваш идентификатор не найден среди подписантов этого документа.",
                                "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Stop);
                DeactivateStampMode();
                return;
            }

            // 2. Вставляем штамп
            SaveToDataBaseRastr(pointContext.DataObject, pointContext.ClickPoint, pointContext.PageNumber);

            DeactivateStampMode();
        }

        /// <summary>
        /// Проверяет, есть ли подпись текущего пользователя на документе.
        /// </summary>
        private bool CanUserAddStamp(IDataObject dataObject)
        {
            if (dataObject == null) return false;

            // 1. Получаем файлы из актуального снимка (как в примере BatchDigitalSigner)
            var files = dataObject.ActualFileSnapshot.Files;

            // 2. Собираем все запросы на подпись со всех файлов
            var allSignatureRequests = files.SelectMany(f => f.SignatureRequests).ToList();

            // 3. Если запросов нет вообще — запрещаем
            if (!allSignatureRequests.Any())
                return false;

            // 4. Получаем список ID ваших должностей
            var myPositionIds = _currentPerson.Positions.Select(p => p.Position).ToList();

            // 5. ПРОВЕРКА: Есть ли хоть один запрос, назначенный на вашу должность?
            // Мы больше не проверяем sig.Signs, только PositionId.
            return allSignatureRequests.Any(sig => myPositionIds.Contains(sig.PositionId));
        }

        private void SaveToDataBaseRastr(IDataObject dataObject, Point position, int pageNumber)
        {
            var builder = _modifier.Edit(dataObject);

            using (var fileStream = File.Open(_filePath, FileMode.Open, FileAccess.Read))
            {
                var positionId = _currentPerson.MainPosition.Position;
                var byteArray = new byte[fileStream.Length];
                fileStream.Read(byteArray, 0, (int)fileStream.Length);

                double imageWidth = 0;
                double imageHeight = 0;

                // Извлекаем размеры изображения в логических единицах (DIP), а не в пикселях
                using (var tempMs = new MemoryStream(byteArray))
                {
                    var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(tempMs,
                        System.Windows.Media.Imaging.BitmapCreateOptions.None,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];

                    // Width и Height возвращают размер в единицах 1/96 дюйма
                    imageWidth = frame.Width;
                    imageHeight = frame.Height;
                }

                var imageStream = new MemoryStream(byteArray);
                var scale = new Point(_scaleXY, _scaleXY);
                var elementId = Guid.NewGuid();

                // Расчет корректного смещения:
                // Вычитаем половину ширины/высоты, умноженную на масштаб, 
                // чтобы точка клика оказалась ровно по центру штампа.
                double correctedX = position.X - (imageWidth * _scaleXY / 2.0);
                double correctedY = position.Y - (imageHeight * _scaleXY / 2.0);

                var element = new GraphicLayerElement(
                    elementId,
                    ToGuid(_currentPerson.Id),
                    positionId,
                    scale,
                    _angle,
                    GraphicLayerElementConstants.BITMAP,
                    pageNumber,
                    true
                )
                {
                    OffsetX = correctedX,
                    OffsetY = correctedY,
                    // Используем Top/Left, так как мы сами вычислили координаты угла
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                // ВАЖНО: Принудительно обновляем ContentId элемента, чтобы он был уникальным
                // Это заставит Pilot воспринимать картинку как новый ресурс
                element.ContentId = Guid.NewGuid();

                // Сериализация и сохранение
                var serializer = new XmlSerializer(typeof(GraphicLayerElement));
                using (var stream = new MemoryStream())
                {
                    serializer.Serialize(stream, element);
                    stream.Position = 0;

                    // Формируем уникальные имена файлов для хранилища Pilot
                    var layerFileName = GraphicLayerElementConstants.GRAPHIC_LAYER_ELEMENT + elementId;

                    // Теперь имя файла контента будет всегда разным, 
                    // и Pilot сразу подтянет новый файл из настроек
                    var contentFileName = GraphicLayerElementConstants.GRAPHIC_LAYER_ELEMENT_CONTENT + element.ContentId;

                    builder.AddFile(layerFileName, stream, DateTime.Now, DateTime.Now, DateTime.Now);
                    builder.AddFile(contentFileName, imageStream, DateTime.Now, DateTime.Now, DateTime.Now);
                }

                _modifier.Apply();
            }
        }

        public static Guid ToGuid(int value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        #endregion

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}