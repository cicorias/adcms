Azure Data Center Migration Solution (adcms)
============================================

##Introduction
To ease migrations, Persistent Systems, along with the Microsoft Customer Advisory Team, has developed the open source Azure Data Center Migration Solution for automatically copying an entire infrastructure as a service (IaaS) deployment on Azure from one location to another. 

With the Azure Data Center Migration Solution version 1.0, you can automate the migration of Microsoft Azure resources:

* From one subscription to another subscription in the same data center (region).
* From one subscription to another subscription in different data centers.
* In the same subscription with different data centers.
* In the same subscription with the same data center.

You can migrate all of the following resources in the source data center:

* Affinity groups
* Networks
* Cloud services
* Storage accounts 
* Virtual machines (VMs)

##User Guide
Please refer to the [User Guide](/User Guide.docx) for instructions on how the use the solution.
It also describes the solution architecture and how the solution takes care of important considerations like atomicity, consistency and extensibity.

##Contributors

###Persistent Systems: 
Satish Nikam, Maryann Fernandes, Shubhangi Pote, Neelam Sahu, Divya M

###Microsoft: 
Rangarajan Srirangam, Guy Bowerman, Suren Machiraju, Yohirito Tada

##License
The content of this repository is licensed under the Apache License, Version 2.0. Please find a copy of the license at [License.rtf](/License.rtf).

##Issues / Feedback
You may report issues or submit feature requests in the [Issues](https://github.com/persistentsystems/adcms/issues) section of this repository. 
For questions or feedback about the solution, please contact [adcms@persistent.com](mailto:adcms@persistent.com).
