using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using Ascon.Pilot.SDK.Controls;
using Ascon.Pilot.SDK.Toolbar;
using Ascon.Pilot.Theme.ColorScheme;
using Ascon.Pilot.SDK.GraphicLayerSample;
using System.Linq;

namespace Ascon.Pilot.SDK.GraphicLayerSample
{
    [Export(typeof(IToolbar<XpsRenderContext>))]
    public class GraphicLayerToolbar : IToolbar<XpsRenderContext>
    {
        private const string TbiToggleGraphicLayer = "tbiToggleGraphicLayer";

        private readonly IObjectsRepository _repository;
        private readonly IXpsViewer _xpsViewer;

        private IToolbarBuilder _builder;
        private bool _isGraphicLayerVisible;

        [ImportingConstructor]
        public GraphicLayerToolbar(IObjectsRepository repository, IXpsViewer xpsViewer, IPilotDialogService dialogService)
        {
            _repository = repository;
            _xpsViewer = xpsViewer;

            // Инициализация цвета тулбара
            var convertFromString = ColorConverter.ConvertFromString(dialogService.AccentColor);
            if (convertFromString != null)
            {
                var color = (Color)convertFromString;
                ColorScheme.Initialize(color, dialogService.Theme);
            }

            // Загружаем состояние слоя из настроек
            _isGraphicLayerVisible = Properties.Settings.Default.IsGraphicLayerEnabled;
            // 🔥 подписка на глобальное событие
            GraphicLayerEvents.GraphicLayerToggled += OnGlobalGraphicLayerToggled;
        }

        private void OnGlobalGraphicLayerToggled(bool isEnabled)
        {
            _isGraphicLayerVisible = isEnabled;

            // сохраняем в настройки
            Properties.Settings.Default.IsGraphicLayerEnabled = isEnabled;
            Properties.Settings.Default.Save();

            // обновляем кнопку на тулбаре
            if (_builder != null)
                AddOrReplaceToggleButtonItem(_builder, TbiToggleGraphicLayer, isEnabled);
        }


        public void Build(IToolbarBuilder builder, XpsRenderContext context)
        {
            _builder = builder;
            bool isVisible = _settingsModel?.IsGraphicLayerEnabled ?? _isGraphicLayerVisible;

            AddOrReplaceToggleButtonItem(_builder, TbiToggleGraphicLayer, _isGraphicLayerVisible)
                .WithIcon(IconLoader.GetIcon("add_graphic_layer.svg"))
                .WithIsEnabled(context.IsDocumentLoaded)
                .WithHeader("Graphic Layer")
                .WithShowHeader(false)
                .WithHint("Toggle Graphic Layer");
        }

        public void OnToolbarItemClick(string name, XpsRenderContext context)
        {
            if (name == TbiToggleGraphicLayer)
            {
                // Меняем состояние модели, чтобы событие синхронизировало кнопку и настройки
                if (_settingsModel != null)
                {
                    _settingsModel.IsGraphicLayerEnabled = !_settingsModel.IsGraphicLayerEnabled;
                }
                else
                {
                    // Если модель не привязана, сохраняем в локальное поле
                    _isGraphicLayerVisible = !_isGraphicLayerVisible;
                    Properties.Settings.Default.IsGraphicLayerEnabled = _isGraphicLayerVisible;
                    Properties.Settings.Default.Save();
                    AddOrReplaceToggleButtonItem(_builder, TbiToggleGraphicLayer, _isGraphicLayerVisible);
                }
            }
        }


        private IToolbarButtonItemBuilder AddOrReplaceToggleButtonItem(IToolbarBuilder builder, string name, bool state)
        {
            return builder.ItemNames.Contains(name)
                ? builder.ReplaceToggleButtonItem(name).WithIsChecked(state)
                : builder.AddToggleButtonItem(name, builder.Count).WithIsChecked(state);
        }

        private static Guid ToGuid(int value)
        {
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(value).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        private GraphicLayerElementSettingsModel _settingsModel;

        public void AttachSettingsModel(GraphicLayerElementSettingsModel model)
        {
            if (_settingsModel != null)
                _settingsModel.GraphicLayerEnabledChanged -= OnGraphicLayerEnabledChanged;

            _settingsModel = model;
            _settingsModel.GraphicLayerEnabledChanged += OnGraphicLayerEnabledChanged;
        }

        private void OnGraphicLayerEnabledChanged(object sender, bool isEnabled)
        {
            _isGraphicLayerVisible = isEnabled;

            Properties.Settings.Default.IsGraphicLayerEnabled = isEnabled;
            Properties.Settings.Default.Save();

            if (_builder != null)
                AddOrReplaceToggleButtonItem(_builder, TbiToggleGraphicLayer, isEnabled);
        }

    }



}
