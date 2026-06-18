using System;
using System.ComponentModel.Composition;
using Ascon.Pilot.SDK;
using Ascon.Pilot.SDK.Menu;

namespace Ascon.Pilot.SDK.GraphicLayerSample
{
    [Export(typeof(IMenu<MainViewContext>))]
    public class GraphicLayerMainMenu : IMenu<MainViewContext>
    {
        private const string TOGGLE_LAYER = "ToggleGraphicLayer";
        private readonly IObjectsRepository _repository;

        [ImportingConstructor]
        public GraphicLayerMainMenu(IObjectsRepository repository)
        {
            _repository = repository;
        }

        void IMenu<MainViewContext>.Build(IMenuBuilder builder, MainViewContext context)
        {
            // Добавляем обычный пункт меню
            builder.AddItem(TOGGLE_LAYER, -1)
                   .WithHeader("Переключить графический слой");
        }

        void IMenu<MainViewContext>.OnMenuItemClick(string name, MainViewContext context)
        {
            if (name != TOGGLE_LAYER)
                return;

            // Переключаем состояние слоя
            bool newState = !Properties.Settings.Default.IsGraphicLayerEnabled;
            Properties.Settings.Default.IsGraphicLayerEnabled = newState;
            Properties.Settings.Default.Save();

            // ⚠️ визуальное обновление XPS здесь недоступно
        }
    }
}
