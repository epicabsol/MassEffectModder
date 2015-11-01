﻿/*
 * METexturesExplorer
 *
 * Copyright (C) 2015 Pawel Kolodziejski <aquadran at users.sourceforge.net>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 *
 */


using System.Collections.Generic;
using System.Linq;

namespace METexturesExplorer
{
    class Texture
    {
        public Texture(Package package, int exportId)
        {
            TexProperty properties = new TexProperty(package, exportId);
            foreach (TexProperty.TexPropertyEntry prop in properties.texPropertyList)
            {
                if (prop.name == "None")
                    continue;
                properties.fetchValue(prop.name);
            }
        }
    }
}