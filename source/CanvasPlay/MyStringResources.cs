using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  文字列リソース.
    /// </summary>
    public class MyStringResources
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public MyStringResources()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                return;
            }

            m_resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader();
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="name">リソースファイルの名前</param>
        public MyStringResources(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException();
            }
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                return;
            }

//            m_resourceLoader = new Windows.ApplicationModel.Resources.ResourceLoader(name);
            m_resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView(name);
        }

        /// <summary>
        ///  リソースファイルの名前.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set
            {
                if (String.Compare(value, m_name, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    m_name = value;
                    m_resourceLoader = String.IsNullOrEmpty(value) ?
                        Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView() : 
                        Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView(value);
                }
            }
        }

        /// <summary><see cref="Name"/>プロパティ</summary>
        private string m_name;

        /// <summary>
        /// リソース.
        /// </summary>
        /// <param name="key">リソース名.</param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                {
                    return key;
                }
                return this.m_resourceLoader.GetString(key);
            }
        }

        /// <summary>
        ///  リソース.
        /// </summary>
        private Windows.ApplicationModel.Resources.ResourceLoader m_resourceLoader;
    }
}
