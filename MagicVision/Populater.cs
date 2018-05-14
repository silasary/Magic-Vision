using MagicVision.DataClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MagicVision.MainForm;

namespace MagicVision
{
    class Populater
    {
        public void UpdateCard(ReferenceCard card)
        {
            var directory = Path.Combine(refCardDir, (string)card.dataRow["Set"]);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var image = Path.Combine(directory, card.CollectorNumber + ".jpg");
            if (!File.Exists(image))
            {

            }

            Phash.ph_dct_imagehash(image, ref card.pHash);
            sql.dbNone("UPDATE cards SET pHash=" + card.pHash.ToString() + " WHERE id=" + card.Id);
        }


    }
}
