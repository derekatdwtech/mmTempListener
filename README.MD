# Tempaast Temperature Receiving Function

## Infrastructure Required
* Azure Function (Consumption Plan)
* Azure Storage Queue
  * temperature
  * alerts 

## Application Settings 
|Name |Value |
|--- |---| 
|defaultAlertTemp | `int` |
|meatMonitorApi | `string` |
|alertQueue | `string` |