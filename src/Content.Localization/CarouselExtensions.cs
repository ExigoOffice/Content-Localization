using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Localization
{
    public static class CarouselExtensions
    {
        private const string _exigoCarousel = @"<div id=""{NAME}"" class=""carousel slide"" data-ride=""carousel"">
            <div class=""carousel-inner"">{CAROUSELITEMS}</div>
            <ol class=""carousel-indicators"">{CAROUSELLEAFS}</ol>
            <a class=""{CAROUSELPREV}"" href=""#{NAME}"" role=""button"" data-slide=""prev"">
                <span class=""{CAROUSELPREVICON}"" aria-hidden=""true""></span>
                <span class=""sr-only"">Previous</span>
            </a>
            <a class=""{CAROUSELNEXT}"" href=""#{NAME}"" role=""button"" data-slide=""next"">
                <span class=""{CAROUSELNEXTICON}"" aria-hidden=""true""></span>
                <span class=""sr-only"">Next</span>
            </a>
        </div>";

        private const string _carouselLeaf =
            @"<li data-target=""#{NAME}"" data-slide-to=""{SLIDENO}"" class=""{TOGGLEACTIVE}""></li>";

        private const string _carouselItem = @"
            <div class=""{CAROUSELITEM} {TOGGLEACTIVE}"">
                {BANNER}
            </div>
        ";
        
        //for runtime performance, put regex in static references. 
        static readonly Regex _attributesRegex   = new Regex("(?<=<exigocarouselattributes.*type=\").*?(?=\" />)");
        static readonly Regex _bannerRegex       = new Regex("(?<=<exigobanner name=\").*?(?=\" />)");
        static readonly Regex _datasetRegex      = new Regex("(?<=<dataset name=\").*?(?=\" />)");
        public static string GenerateCarousel(this IContentLocalizer localizer, string name, Dictionary<string,string> filters)
        {
            if (localizer is null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }


            var carousel = localizer[name];
            if (!(carousel?.Length > 0)) return "";

            var carouselItems = new List<string>();
            var carouselType = _attributesRegex.Match(carousel).Value;

            foreach (Match bannerName in _bannerRegex.Matches(carousel))
            {
                bool? skip = null;
                var banner = localizer[bannerName.Value];

                foreach (Match setName in _datasetRegex.Matches(banner))
                {
                    var dataSet = localizer[setName.Value];
                    var set = dataSet.Split(':')[0];
                    var values = dataSet.Split(':')[1].Split(',').ToArray();
                    if (filters!=null && filters.ContainsKey(set) && filters[set] != null && skip != false)
                        skip = values.FirstOrDefault(x => x == filters[set]) == null;
                }

                if (skip == true) continue;

                carouselItems.Add(banner);
            }
            return MergeCarouselContents(name, carouselType, carouselItems);
        }

        private static string MergeCarouselContents(string name, string type, IEnumerable<string> items)
        {
            var carouselItems = new StringBuilder();
            var carouselLeafs = new StringBuilder();
            var c = 0;
            foreach (var i in items)
            {
                carouselLeafs.Append(_carouselLeaf
                        .Replace("{TOGGLEACTIVE}", c == 0 ? "active" : "")
                        .Replace("{NAME}", name))
                    .Replace("{SLIDENO}", c.ToString(CultureInfo.InvariantCulture));

                carouselItems.Append(_carouselItem
                    .Replace("{TOGGLEACTIVE}", c == 0 ? "active" : "")
                    .Replace("{BANNER}", i));
                c++;
            }


            if (!string.IsNullOrWhiteSpace(type) && type.ToUpper(CultureInfo.InvariantCulture) == "BOOTSTRAP4")
                return _exigoCarousel
                    .Replace("{NAME}", name)
                    .Replace("{CAROUSELITEMS}", carouselItems.ToString())
                    .Replace("{CAROUSELLEAFS}", carouselLeafs.ToString())
                    .Replace("{CAROUSELPREV}", "carousel-control-prev")
                    .Replace("{CAROUSELPREVICON}", "carousel-control-prev-icon")
                    .Replace("{CAROUSELNEXT}", "carousel-control-next")
                    .Replace("{CAROUSELNEXTICON}", "carousel-control-next-icon")
                    .Replace("{CAROUSELITEM}", "carousel-item");
            else //for now assume bootstrap 3
                return _exigoCarousel
                    .Replace("{NAME}", name)
                    .Replace("{CAROUSELITEMS}", carouselItems.ToString())
                    .Replace("{CAROUSELLEAFS}", carouselLeafs.ToString())
                    .Replace("{CAROUSELPREV}", "left carousel-control")
                    .Replace("{CAROUSELPREVICON}", "glyphicon glyphicon-chevron-left")
                    .Replace("{CAROUSELNEXT}", "right carousel-control")
                    .Replace("{CAROUSELNEXTICON}", "glyphicon glyphicon-chevron-right")
                    .Replace("{CAROUSELITEM}", "item");
        }

    }
}
