# Licences

The spirit is to have DocIntel as open-source as possible while enabling its integration with other tools and systems, ensuring its long-term perinity and avoiding any vendor locking or abuse by third party commercial entities.

The principles are the following

* The **core** of DocIntel (DocIntel.Core) is published as LGPLv3 in order to allow the interfaces and core features to be integrated in other tools, that are not necessarily open source. The **integration** libraries follow the same principles.
* The **services** of DocIntel (DocIntel.Services.*) are published as AGPLv3 to ensure that any contributions to these services are shared back with the community, even if used in a SaaS model. Any third party services is allowed to replace these components with others, connecting to the message bus and working as external components. The interfaces are part of the core and covered above. 
* The **web application** is also published as AGPLv3 to ensure that no SaaS provider can modify the interface without sharing back with the community.
* The **consoles** are published as GPLv3 to ensure that no one can modify the software without sharing back with the community. Since there is no interaction over the network, there is no reason for enforcing AGPLv3.
* The **API clients** are published under Apache Licence 2.0 to minimize possible licence friction with other tools and systems that wishes to integrate with DocIntel eco-system.

All contributors are required to fill a DCA. We choosed DCA over CLO for the reasons explained in [this excellent blog post](https://writing.kemitchell.com/2021/07/02/DCO-Not-CLA). The GitHub bot will ensure that everything is in order.

# LGPLv3

	DocIntel.Core, DocIntel.Integrations
	Copyright (C) 2018-2022 Antoine Cailliau
	Copyright (C) 2019-2022 Belgian Defense
	
	This program is free software: you can redistribute it and/or modify 
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.
	
	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.
	
	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
	
# GPLv3

	DocIntel.Console, DocIntel.AdminConsole
	Copyright (C) 2018-2022 Antoine Cailliau
	Copyright (C) 2019-2022 Belgian Defense
	
	This program is free software: you can redistribute it and/or modify 
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.
	
	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.
	
	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
	
# AGPLv3

	DocIntel.Services.*, DocIntel.WebApp
	Copyright (C) 2018-2022 Antoine Cailliau
	Copyright (C) 2019-2022 Belgian Defense
	
	This program is free software: you can redistribute it and/or modify 
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.
	
	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.
	
	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
	
# Apache Licence 2.0

	Copyright 2018-2022 Antoine Cailliau
	Copyright 2019-2022 Belgian Defense

	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at

		http://www.apache.org/licenses/LICENSE-2.0

	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.